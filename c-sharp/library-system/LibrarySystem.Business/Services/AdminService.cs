using LibrarySystem.Contracts.Interfaces;
using LibrarySystem.Data;
using LibrarySystem.Models;
using LibrarySystem.Business.Exceptions;

namespace LibrarySystem.Business.Services;

public class AdminService : IAdminService
{
    private readonly ILibraryRepository _repository;
    private readonly IAuthService _authService;

    public AdminService(ILibraryRepository repository, IAuthService authService)
    {
        _repository = repository;
        _authService = authService;
    }

    public void UpdateFineAmount(string fineType, decimal newAmount)
    {
        var config = _repository.GetFineConfigByType(fineType);
        if (config == null)
        {
            throw new ValidationException($"Fine type '{fineType}' not found.");
        }

        if (newAmount < 0)
        {
            throw new ValidationException("Fine amount cannot be negative.");
        }

        config.Amount = newAmount;
        _repository.UpdateFineConfig(config);
        _repository.SaveChanges();
    }

    public void RegisterAdmin(string name, string phone, string email, string password)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ValidationException("Admin name is required.");
        
        var admin = new Admin
        {
            Adminname = name,
            Adminphone = phone,
            Adminemail = email
        };
        _repository.AddAdmin(admin);
        _repository.SaveChanges();

        // 2. Add Password Record
        var passwordRecord = new Password
        {
            Userid = $"adm_{admin.Adminid}",
            Passwordhash = _authService.HashPassword(password)
        };
        _repository.AddPasswordRecord(passwordRecord);
        _repository.SaveChanges();
    }

    public Admin? Login(string input)
    {
        return _repository.GetAllAdmins().FirstOrDefault(a => a.Adminemail == input || a.Adminname == input);
    }

    public IEnumerable<Fineconfiguration> GetFineConfiguration()
    {
        return _repository.GetFineConfigs();
    }

    public IEnumerable<Return> GetPendingReturns()
    {
        return _repository.GetPendingReturns();
    }

    public void ApproveReturn(int returnId, string finalCondition)
    {
        if (finalCondition != "Good" && finalCondition != "Damaged")
        {
            throw new ValidationException("Invalid condition. Must be 'Good' or 'Damaged'.");
        }

        var ret = _repository.GetReturnById(returnId);
        if (ret == null) throw new ValidationException("Return record not found.");

        var borrowing = _repository.GetBorrowingById(ret.Borrowid ?? 0);
        if (borrowing == null) throw new ValidationException("Associated borrowing not found.");

        var copy = _repository.GetCopiesByBookId(borrowing.Bookid ?? 0).FirstOrDefault(c => c.Copystatus == "Unavailable");
        if (copy == null) throw new ValidationException("Book copy not found or not in review state.");

        try
        {
            _repository.BeginTransaction();

            // 1. Calculate Fines
            decimal lateFee = 0;
            decimal damageFee = 0;

            // Late Fine calculation
            DateTime actualReturnDate = ret.Actualreturndate?.ToDateTime(TimeOnly.MinValue) ?? DateTime.Now;
            DateTime dueDate = borrowing.Duedate.ToDateTime(TimeOnly.MinValue);
            if (actualReturnDate.Date > dueDate)
            {
                var lateConfig = _repository.GetFineConfigByType("LateReturn");
                int daysLate = (actualReturnDate.Date - dueDate).Days;
                lateFee = (lateConfig?.Amount ?? 10) * daysLate;
            }

            // Damage condition update and fee
            bool isNewlyDamaged = (finalCondition == "Damaged"); // We always check if it's damaged now
            copy.Copycondition = finalCondition;
            copy.Copystatus = "Available";
            _repository.UpdateBookCopy(copy);

            if (isNewlyDamaged)
            {
                var damageConfig = _repository.GetFineConfigByType("Damaged");
                damageFee = damageConfig?.Amount ?? 50;
            }

            decimal totalFine = lateFee + damageFee;

            // 2. Finalize Return Record
            ret.Fineamount = totalFine;
            ret.Returnapprovalstatus = "Approved";
            _repository.UpdateReturn(ret);

            // 3. Finalize Borrowing Record
            borrowing.Returnstatus = "Returned";
            _repository.UpdateBorrowing(borrowing);

            // 4. Calculate Remarks
            var remarksList = new List<string>();
            if (lateFee > 0) remarksList.Add("Late Return");
            if (damageFee > 0) remarksList.Add("Damaged Book");
            string remarks = remarksList.Count > 0 ? string.Join(" and ", remarksList) : "Standard Return";

            // 5. Log Fine if any
            if (totalFine > 0)
            {
                _repository.AddFine(new Finecalculation
                {
                    Borrowid = ret.Borrowid,
                    Fineamount = totalFine,
                    Finestatus = "Unpaid",
                    Remarks = remarks
                });
            }

            _repository.SaveChanges();
            _repository.CommitTransaction();
        }
        catch
        {
            _repository.RollbackTransaction();
            throw;
        }
    }

    public void RejectReturn(int returnId, string remark)
    {
        var ret = _repository.GetReturnById(returnId);
        if (ret == null) throw new ValidationException("Return record not found.");

        var borrowing = _repository.GetBorrowingById(ret.Borrowid ?? 0);
        if (borrowing == null) throw new ValidationException("Associated borrowing not found.");

        var copy = _repository.GetCopiesByBookId(borrowing.Bookid ?? 0).FirstOrDefault(c => c.Copystatus == "Unavailable");
        if (copy == null) throw new ValidationException("Book copy not found or not in review state.");

        try
        {
            _repository.BeginTransaction();

            // 1. Mark return as Rejected
            ret.Returnapprovalstatus = "Rejected";
            _repository.UpdateReturn(ret);

            // 2. Add remark to borrowing
            borrowing.Remarks = remark;
            _repository.UpdateBorrowing(borrowing);

            // 3. Rollback copy status to Borrowed
            copy.Copystatus = "Borrowed";
            _repository.UpdateBookCopy(copy);

            _repository.SaveChanges();
            _repository.CommitTransaction();
        }
        catch
        {
            _repository.RollbackTransaction();
            throw;
        }
    }
}
