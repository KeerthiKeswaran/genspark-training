namespace WordleGame.Interfaces
{
    public interface IValidationService
    {
        void Validate(string guess);
        int ValidateDifficulty(string input);
    }
}
