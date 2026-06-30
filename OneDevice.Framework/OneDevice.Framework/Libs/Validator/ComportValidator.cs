using System.Text.RegularExpressions;

namespace OneDevice.Framework.Libs.Validator;

public class ComportValidator : IValidator
{
    private static readonly Regex ComValidationRegex = new(@"^(COM\d+){1};?(\d+)?$", RegexOptions.Compiled);
    public Regex ValidationRegex => ComValidationRegex;

    public bool Validate(string inputString)
    {
        if (string.IsNullOrEmpty(inputString)) return false;

        return ValidationRegex.IsMatch(inputString);
    }

    public string GetExample()
    {
        return "COM23;19200";
    }
}