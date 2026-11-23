using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Farmer.Contracts;

namespace Farmer.Service.Validation
{
    /// <summary>Server-side validation rules. The service never trusts the client UI.</summary>
    public static class InputValidator
    {
        private static readonly Regex EmailRegex =
            new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled);
        private static readonly Regex PhoneRegex =
            new Regex(@"^\+?[0-9]{9,15}$", RegexOptions.Compiled);

        public static readonly string[] AllowedUnits = { "kg", "g", "piece", "bunch", "bag", "litre" };

        public static List<string> ValidateRegistration(FarmerRegistration r)
        {
            var e = new List<string>();
            if (r == null) { e.Add("Registration data is required."); return e; }
            Required(e, r.FullName, "Full name", 100);
            Required(e, r.Email, "Email", 150);
            if (!string.IsNullOrWhiteSpace(r.Email) && !EmailRegex.IsMatch(r.Email.Trim()))
                e.Add("Email address is not valid.");
            string phone = (r.Phone ?? "").Replace(" ", "").Replace("-", "");
            if (!PhoneRegex.IsMatch(phone)) e.Add("Phone must contain 9-15 digits (optional leading +).");
            Required(e, r.District, "District", 60);
            if (!string.IsNullOrEmpty(r.Address) && r.Address.Length > 250) e.Add("Address is too long (max 250).");
            if (string.IsNullOrEmpty(r.Password) || r.Password.Length < 8
                || !r.Password.Any(char.IsLetter) || !r.Password.Any(char.IsDigit))
                e.Add("Password must be at least 8 characters and contain letters and digits.");
            return e;
        }

        public static List<string> ValidateSupplyRequest(SupplyRequestInput i)
        {
            var e = new List<string>();
            if (i == null) { e.Add("Supply request data is required."); return e; }
            if (i.FarmerId <= 0) e.Add("FarmerId is required.");
            Required(e, i.ItemName, "Item name", 100);
            if (i.Quantity <= 0 || i.Quantity > 1000000) e.Add("Quantity must be between 1 and 1,000,000.");
            Required(e, i.DeliveryLocation, "Delivery location", 200);
            if (i.PreferredDate.Date < DateTime.Today) e.Add("Preferred date cannot be in the past.");
            return e;
        }

        public static List<string> ValidateCropListing(CropListingInput i)
        {
            var e = new List<string>();
            if (i == null) { e.Add("Crop listing data is required."); return e; }
            if (i.FarmerId <= 0) e.Add("FarmerId is required.");
            Required(e, i.CropName, "Crop name", 100);
            if (i.Quantity <= 0 || i.Quantity > 10000000m) e.Add("Quantity must be greater than 0.");
            if (i.UnitPrice <= 0 || i.UnitPrice > 100000000m) e.Add("Unit price must be greater than 0.");
            if (string.IsNullOrWhiteSpace(i.Unit) || !AllowedUnits.Contains(i.Unit.Trim().ToLowerInvariant()))
                e.Add("Unit must be one of: " + string.Join(", ", AllowedUnits) + ".");
            Required(e, i.ContactInfo, "Contact info", 150);
            if (!string.IsNullOrEmpty(i.Notes) && i.Notes.Length > 500) e.Add("Notes are too long (max 500).");
            if (i.AvailabilityDate == default(DateTime)) e.Add("Availability date is required.");
            return e;
        }

        private static void Required(List<string> errors, string value, string field, int maxLength)
        {
            if (string.IsNullOrWhiteSpace(value)) errors.Add(field + " is required.");
            else if (value.Trim().Length > maxLength) errors.Add(field + " is too long (max " + maxLength + ").");
        }
    }
}
