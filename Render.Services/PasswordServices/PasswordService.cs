using System.Text;
using System.Text.RegularExpressions;

namespace Render.Services.PasswordServices;

public class PasswordService : IPasswordService
{
    /// <summary>
    /// Generates a password that conforms to our criteria
    /// </summary>
    /// <returns>The password.</returns>
    public string GeneratePassword()
    {
        var wordSourcePath = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location)
                             + @"\PasswordServices\data\PasswordStrings.txt";

        var lines = File.ReadAllLines(wordSourcePath);
        var words = new List<string>(lines);
        var randomPassword = new StringBuilder();
        var random = new Random();
        var wordsAdded = 0;
        while (wordsAdded < 3 || randomPassword.Length < 16)
        {
            var index = random.Next(words.Count);
            var letters = words[index].ToCharArray();
            letters[0] = char.ToUpper(letters[0]);
            var word = new string(letters);
            //Skip adding the word if it causes there to be more than 3 instances of a character in a row
            if (Regex.IsMatch(randomPassword + word, @"(\S)\1{3,}"))
            {
                continue;
            }

            randomPassword.Append(word);
            wordsAdded++;
        }

        var bytes = Encoding.Unicode.GetBytes(randomPassword.ToString());
        return Encoding.Unicode.GetString(bytes).Normalize();
    }
}