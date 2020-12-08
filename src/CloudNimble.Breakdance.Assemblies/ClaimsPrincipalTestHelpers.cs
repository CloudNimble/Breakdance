using System.Collections.Generic;

namespace System.Security.Claims
{

    /// <summary>
    /// A set of helpers to make it easier to test code that calls <see cref="ClaimsPrincipal.Current" />.
    /// </summary>
    public static class ClaimsPrincipalTestHelpers
    {

        /// <summary>
        /// Sets the <see cref="ClaimsPrincipal.ClaimsPrincipalSelector"/> to a new ClaimsIdentity with the specified claims.
        /// </summary>
        /// <param name="claims">The Claims to set for the test run.</param>
        /// <param name="authenticationType">If needed, the AuthenticationType of the ClaimsIdentity. Defaults to "BreakdanceTests".</param>
        /// <param name="nameType">The ClaimType to specify for the Name claim. Defaults to <see cref="ClaimTypes.NameIdentifier"/>.</param>
        /// <param name="roleType">The ClaimType to specify for Role claims. Defaults to <see cref="ClaimTypes.Role"/>.</param>
        public static void SetSelector(List<Claim> claims, string authenticationType = "BreakdanceTests", string nameType = ClaimTypes.NameIdentifier, string roleType = ClaimTypes.Role)
        {
            ClaimsPrincipal.ClaimsPrincipalSelector = () => new ClaimsPrincipal(new ClaimsIdentity(claims, authenticationType, nameType, roleType));
        }

    }

}
