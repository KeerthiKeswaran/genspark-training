using server.Core.Enums;
using server.Contracts.Interfaces;
using System;
using System.Threading.Tasks;

namespace server.Features.Administration
{
    public class OperatorWorkflowService : IOperatorWorkflowService
    {
        private readonly IOperatorRepository _operatorRepository;
        private readonly INotificationService _notificationService;

        public OperatorWorkflowService(IOperatorRepository operatorRepository, INotificationService notificationService)
        {
            _operatorRepository = operatorRepository;
            _notificationService = notificationService;
        }

        public async Task<(bool Success, string Message)> DeactivateOperatorAsync(Guid operatorId)
        {
            var op = await _operatorRepository.GetOperatorByIdAsync(operatorId);
            if (op == null) return (false, "Operator not found");

            op.IsApproved = false;
            op.Status = ApprovalStatus.Deactivated;

            var futureSchedules = await _operatorRepository.GetFutureSchedulesByOperatorIdAsync(operatorId);

            foreach (var schedule in futureSchedules)
            {
                schedule.Status = JourneyStatus.Cancelled;

                var affectedBookings = await _operatorRepository.GetConfirmedBookingsByScheduleIdAsync(schedule.Id);

                foreach (var booking in affectedBookings)
                {
                    booking.Status = BookingStatus.Cancelled;
                    if (booking.Payment != null && booking.Payment.Status == PaymentStatus.Success)
                    {
                        booking.Payment.Status = PaymentStatus.Refunded;
                        booking.Payment.ProcessedAt = DateTime.UtcNow;
                    }

                    if (booking.Customer != null)
                    {
                        await _notificationService.SendEmailAsync(
                            booking.Customer.Email,
                            "Trip Cancelled & Refund Initiated",
                            $"Your upcoming trip on {schedule.DepartureTime} has been cancelled due to operational reasons. A full refund of ₹{booking.TotalAmount + booking.PlatformFee} has been initiated."
                        );
                    }
                }
            }

            await _notificationService.SendEmailAsync(
                op.Email,
                "Operator Account Deactivated",
                "Your operator account has been deactivated by the platform administrators. All future trips have been cancelled and customers refunded."
            );

            await _notificationService.NotifyUserAsync(op.Id, "Account Deactivated", "Your account has been deactivated. All future trips were cancelled.", "Deactivation");

            await _operatorRepository.SaveChangesAsync();

            return (true, $"Operator {op.CompanyName} deactivated. {futureSchedules.Count} trips cancelled.");
        }

        public async Task<(bool Success, string Message)> ReactivateOperatorAsync(Guid operatorId)
        {
            var op = await _operatorRepository.GetOperatorByIdAsync(operatorId);
            if (op == null) return (false, "Operator not found");

            if (op.IsApproved) return (false, "Operator is already active");

            op.IsApproved = true;
            op.Status = ApprovalStatus.Approved;
            await _operatorRepository.UpdateOperatorAsync(op);
            
            await _notificationService.SendEmailAsync(
                op.Email,
                "Operator Account Reactivated",
                "Your operator account has been reactivated. You can now resume operations and schedule new trips."
            );

            await _notificationService.NotifyUserAsync(op.Id, "Account Reactivated", "Your operator account has been reactivated successfully.", "Activation");

            await _operatorRepository.SaveChangesAsync();
            Console.WriteLine($"[Workflow] Operator {operatorId} reactivated.");
            return (true, $"Operator {op.CompanyName} reactivated successfully.");
        }
    }
}
