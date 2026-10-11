

namespace skolenza.application.Interfaces.IHashed
{
   
        public interface IPasswordService
        {
            string HashPassword(string password);
            bool  VerifyPassword(string hashedPassword, string enteredPassword);
        }

    
}
