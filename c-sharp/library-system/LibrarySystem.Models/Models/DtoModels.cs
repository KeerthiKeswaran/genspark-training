namespace LibrarySystem.Models.Models;

public record AvailableBookResult(int b_id, string b_title);
public record MemberBorrowingSummaryResult(long active_loans, long total_borrowed, decimal unpaid_fines);
public record MostBorrowedBookResult(string Booktitle, long Borrowcount);
