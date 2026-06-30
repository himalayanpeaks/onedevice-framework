using System.Text.RegularExpressions;

namespace OneDevice.Framework.Libs.Validator;

public interface IValidator
{
    Regex ValidationRegex { get; }
    bool Validate(string inputString);
    string GetExample();
}