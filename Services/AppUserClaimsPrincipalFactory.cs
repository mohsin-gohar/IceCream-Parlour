using System.Security.Claims;
using Ice_Cream_Parlour_Eproject.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace Ice_Cream_Parlour_Eproject.Services
{
    /// <summary>
    /// Adds frequently used profile fields to the authentication cookie so the layout
    /// does not have to query the database for every request. Deriving from the
    /// two-generic base class also keeps role claims intact for [Authorize(Roles=...)].
    /// </summary>
    public class AppUserClaimsPrincipalFactory : UserClaimsPrincipalFactory<User, IdentityRole>
    {
        public const string FullNameClaim = "full_name";
        public const string AvatarClaim = "avatar";
        public const string IsPaidClaim = "is_paid";
        public const string PaymentExpiryClaim = "payment_expiry";

        public AppUserClaimsPrincipalFactory(
            UserManager<User> userManager,
            RoleManager<IdentityRole> roleManager,
            IOptions<IdentityOptions> optionsAccessor)
            : base(userManager, roleManager, optionsAccessor)
        {
        }

        protected override async Task<ClaimsIdentity> GenerateClaimsAsync(User user)
        {
            var identity = await base.GenerateClaimsAsync(user);

            if (!string.IsNullOrWhiteSpace(user.FullName))
            {
                identity.AddClaim(new Claim(FullNameClaim, user.FullName));
            }

            if (!string.IsNullOrWhiteSpace(user.ProfilePicture))
            {
                identity.AddClaim(new Claim(AvatarClaim, user.ProfilePicture));
            }

            identity.AddClaim(new Claim(IsPaidClaim, user.IsPaid ? "true" : "false"));

            if (user.PaymentExpiry.HasValue)
            {
                identity.AddClaim(new Claim(PaymentExpiryClaim,
                    user.PaymentExpiry.Value.ToString("O")));
            }

            return identity;
        }
    }
}
