﻿using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace LootLocker
{
    public class LootLockerValidation
    {
        public static bool ValidateEmail(string email, out string error)
        {
            error = "";

            if (string.IsNullOrWhiteSpace(email))
            {
                error = "Email cannot be blank.";
                return false;
            }

            if (!email.Contains("@"))
            {
                error = "The email you entered was invalid.";
                return false;
            }

            try
            {
                // Normalize the domain
                email = Regex.Replace(email, @"(@)(.+)$", DomainMapper,
                                      RegexOptions.None, TimeSpan.FromMilliseconds(200));

                // Examines the domain part of the email and normalizes it.
                string DomainMapper(Match match)
                {
                    // Use IdnMapping class to convert Unicode domain names.
                    IdnMapping idn = new IdnMapping();

                    // Pull out and process domain name (throws ArgumentException on invalid)
                    string domainName = idn.GetAscii(match.Groups[2].Value);

                    return match.Groups[1].Value + domainName;
                }
            }
            catch (RegexMatchTimeoutException e)
            {
                error = "An error has occurred. Please try again.";
                LootLockerLogger.Log("Regex exception: " + e, LootLockerLogger.LogLevel.Warning);
                return false;
            }
            catch (ArgumentException e)
            {
                error = "An error has occurred. Please try again.";
                LootLockerLogger.Log("Argument exception: " + e, LootLockerLogger.LogLevel.Warning);
                return false;
            }

            try
            {
                bool isValid = Regex.IsMatch(email,
                    @"^(?("")("".+?(?<!\\)""@)|(([0-9a-z]((\.(?!\.))|[-!#\$%&'\*\+/=\?\^`\{\}\|~\w])*)(?<=[0-9a-z])@))" +
                    @"(?(\[)(\[(\d{1,3}\.){3}\d{1,3}\])|(([0-9a-z][-0-9a-z]*[0-9a-z]*\.)+[a-z0-9][\-a-z0-9]{0,22}[a-z0-9]))$",
                    RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(250));

                if (!isValid)
                    error = "The email you entered was invalid.";

                return isValid;
            }
            catch (RegexMatchTimeoutException e)
            {
                error = "The email you entered was invalid.";
                LootLockerLogger.Log("Regex exception: " + e, LootLockerLogger.LogLevel.Warning);
                return false;
            }
        }

        public static bool ValidatePassword(string password, out string error)
        {
            error = "";

            Regex numberCheck = new Regex(@"[0-9]+");
            //Regex uppercaseCheck = new Regex(@"[A-Z]+");
            Regex lengthCheck = new Regex(@".{6,}");

            bool hasNumber = numberCheck.IsMatch(password);
            bool hasUppercase = true;//uppercaseCheck.IsMatch(password);
            bool hasLength = lengthCheck.IsMatch(password);

            bool isValidPassword = hasNumber && hasUppercase && hasLength;

            if (!isValidPassword)
            {
                error = "Your password must contain at least 1 number, and be at least 6 characters in length.";
            }

            return isValidPassword;
        }
    }
}
