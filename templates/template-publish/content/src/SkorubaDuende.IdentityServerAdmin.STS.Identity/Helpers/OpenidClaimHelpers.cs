// Copyright (c) Jan Škoruba. All Rights Reserved.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using IdentityModel;
using IdentityModel.Client;
using SkorubaDuende.IdentityServerAdmin.STS.Identity.Configuration.Constants;

namespace SkorubaDuende.IdentityServerAdmin.STS.Identity.Helpers
{
    public static class OpenIdClaimHelpers
    {
        public static Claim ExtractAddressClaim(OpenIdProfile profile)
        {
            var hasData = false;
            using var stream = new MemoryStream();
            using var w = new Utf8JsonWriter(stream);
            w.WriteStartObject();
            if (!string.IsNullOrWhiteSpace(profile.StreetAddress))
            {
                hasData = true;
                w.WriteString(AddressClaimConstants.StreetAddress, profile.StreetAddress);
            }

            if (!string.IsNullOrWhiteSpace(profile.Locality))
            {
                hasData = true;
                w.WriteString(AddressClaimConstants.Locality, profile.Locality);
            }

            if (!string.IsNullOrWhiteSpace(profile.Region))
            {
                hasData = true;
                w.WriteString(AddressClaimConstants.Region, profile.Region);
            }

            if (!string.IsNullOrWhiteSpace(profile.PostalCode))
            {
                hasData = true;
                w.WriteString(AddressClaimConstants.PostalCode, profile.PostalCode);
            }

            if (!string.IsNullOrWhiteSpace(profile.Country))
            {
                hasData = true;
                w.WriteString(AddressClaimConstants.Country, profile.Country);
            }
            w.WriteEndObject();
            w.Flush();

            var addressJson = Encoding.UTF8.GetString(stream.ToArray());

            return new Claim(JwtClaimTypes.Address, hasData ? addressJson : string.Empty);
        }

        /// <summary>
        /// Map claims to OpenId Profile
        /// </summary>
        /// <param name="claims"></param>
        /// <returns></returns>
        public static OpenIdProfile ExtractProfileInfo(IList<Claim> claims)
        {
            var profile = new OpenIdProfile
            {
                FullName = claims.FirstOrDefault(x => x.Type == JwtClaimTypes.Name)?.Value,
                Website = claims.FirstOrDefault(x => x.Type == JwtClaimTypes.WebSite)?.Value,
                Profile = claims.FirstOrDefault(x => x.Type == JwtClaimTypes.Profile)?.Value
            };

            var address = claims.FirstOrDefault(x => x.Type == JwtClaimTypes.Address)?.Value;

            if (address == null) return profile;

            try
            {
                using var doc = JsonDocument.Parse(address);
                var addressJson = doc.RootElement;
                profile.StreetAddress = addressJson.TryGetString(AddressClaimConstants.StreetAddress);
                profile.Locality = addressJson.TryGetString(AddressClaimConstants.Locality);
                profile.Region = addressJson.TryGetString(AddressClaimConstants.Region);
                profile.PostalCode = addressJson.TryGetString(AddressClaimConstants.PostalCode);
                profile.Country = addressJson.TryGetString(AddressClaimConstants.Country);

            }
            catch (Exception)
            {

            }

            return profile;
        }

        /// <summary>
        /// Get claims to remove
        /// </summary>
        /// <param name="oldProfile"></param>
        /// <param name="newProfile"></param>
        /// <returns></returns>
        public static IList<Claim> ExtractClaimsToRemove(OpenIdProfile oldProfile, OpenIdProfile newProfile)
        {
            var claimsToRemove = new List<Claim>();

            if (string.IsNullOrWhiteSpace(newProfile.FullName) && !string.IsNullOrWhiteSpace(oldProfile.FullName))
            {
                claimsToRemove.Add(new Claim(JwtClaimTypes.Name, oldProfile.FullName));
            }

            if (string.IsNullOrWhiteSpace(newProfile.Website) && !string.IsNullOrWhiteSpace(oldProfile.Website))
            {
                claimsToRemove.Add(new Claim(JwtClaimTypes.WebSite, oldProfile.Website));
            }

            if (string.IsNullOrWhiteSpace(newProfile.Profile) && !string.IsNullOrWhiteSpace(oldProfile.Profile))
            {
                claimsToRemove.Add(new Claim(JwtClaimTypes.Profile, oldProfile.Profile));
            }

            var oldAddressClaim = ExtractAddressClaim(oldProfile);
            var newAddressClaim = ExtractAddressClaim(newProfile);

            if (string.IsNullOrWhiteSpace(newAddressClaim.Value) && !string.IsNullOrWhiteSpace(oldAddressClaim.Value))
            {
                claimsToRemove.Add(oldAddressClaim);
            }

            return claimsToRemove;
        }

        /// <summary>
        /// Get claims to add
        /// </summary>
        /// <param name="oldProfile"></param>
        /// <param name="newProfile"></param>
        /// <returns></returns>
        public static IList<Claim> ExtractClaimsToAdd(OpenIdProfile oldProfile, OpenIdProfile newProfile)
        {
            var claimsToAdd = new List<Claim>();

            if (!string.IsNullOrWhiteSpace(newProfile.FullName) && string.IsNullOrWhiteSpace(oldProfile.FullName))
            {
                claimsToAdd.Add(new Claim(JwtClaimTypes.Name, newProfile.FullName));
            }

            if (!string.IsNullOrWhiteSpace(newProfile.Website) && string.IsNullOrWhiteSpace(oldProfile.Website))
            {
                claimsToAdd.Add(new Claim(JwtClaimTypes.WebSite, newProfile.Website));
            }

            if (!string.IsNullOrWhiteSpace(newProfile.Profile) && string.IsNullOrWhiteSpace(oldProfile.Profile))
            {
                claimsToAdd.Add(new Claim(JwtClaimTypes.Profile, newProfile.Profile));
            }

            var oldAddressClaim = ExtractAddressClaim(oldProfile);
            var newAddressClaim = ExtractAddressClaim(newProfile);

            if (!string.IsNullOrWhiteSpace(newAddressClaim.Value) && string.IsNullOrWhiteSpace(oldAddressClaim.Value))
            {
                claimsToAdd.Add(newAddressClaim);
            }

            return claimsToAdd;
        }

        /// <summary>
        /// Get claims to replace
        /// </summary>
        /// <param name="oldClaims"></param>
        /// <param name="newProfile"></param>
        /// <returns></returns>
        public static IList<Tuple<Claim,Claim>> ExtractClaimsToReplace(IList<Claim> oldClaims, OpenIdProfile newProfile)
        {
            var oldProfile = ExtractProfileInfo(oldClaims);
            var claimsToReplace = new List<Tuple<Claim, Claim>>();

            if (!string.IsNullOrWhiteSpace(newProfile.FullName) && !string.IsNullOrWhiteSpace(oldProfile.FullName))
            {
                if (newProfile.FullName != oldProfile.FullName)
                {
                    var oldClaim = oldClaims.First(x => x.Type == JwtClaimTypes.Name);
                    var newClaim = new Claim(JwtClaimTypes.Name, newProfile.FullName);
                    claimsToReplace.Add(new Tuple<Claim, Claim>(oldClaim, newClaim));
                }
            }

            if (!string.IsNullOrWhiteSpace(newProfile.Website) && !string.IsNullOrWhiteSpace(oldProfile.Website))
            {
                if (newProfile.Website != oldProfile.Website)
                {
                    var oldClaim = oldClaims.First(x => x.Type == JwtClaimTypes.WebSite);
                    var newClaim = new Claim(JwtClaimTypes.WebSite, newProfile.Website);
                    claimsToReplace.Add(new Tuple<Claim, Claim>(oldClaim, newClaim));
                }
            }

            if (!string.IsNullOrWhiteSpace(newProfile.Profile) && !string.IsNullOrWhiteSpace(oldProfile.Profile))
            {
                if (newProfile.Profile != oldProfile.Profile)
                {
                    var oldClaim = oldClaims.First(x => x.Type == JwtClaimTypes.Profile);
                    var newClaim = new Claim(JwtClaimTypes.Profile, newProfile.Profile);
                    claimsToReplace.Add(new Tuple<Claim, Claim>(oldClaim, newClaim));
                }
            }

            var oldAddressClaim = ExtractAddressClaim(oldProfile);
            var newAddressClaim = ExtractAddressClaim(newProfile);

            if (!string.IsNullOrWhiteSpace(newAddressClaim.Value) && !string.IsNullOrWhiteSpace(oldAddressClaim.Value))
            {
                if (newAddressClaim.Value != oldAddressClaim.Value)
                {
                    var oldClaim = oldClaims.First(x => x.Type == JwtClaimTypes.Address);
                    claimsToReplace.Add(new Tuple<Claim, Claim>(oldClaim, newAddressClaim));
                }
            }

            return claimsToReplace;
        }
    }
}








