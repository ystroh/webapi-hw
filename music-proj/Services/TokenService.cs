using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace TokenServices.Services;

public static class TokenService
{
    // מפתח הצפנה סודי לחתימת הטוקנים
    private static readonly SymmetricSecurityKey _key = new(Encoding.UTF8.GetBytes("216228783328328992naamaandyehudit"));
    private static readonly string _issuer = "https://token";

    /// <summary>
    /// יצירת אובייקט טוקן המכיל את נתוני המשתמש (Claims) ותוקף ל-30 יום
    /// </summary>
    public static SecurityToken GetToken(List<Claim> claims) =>
        new JwtSecurityToken(
            _issuer,
            _issuer,
            claims,
            expires: DateTime.Now.AddDays(30.0),
            signingCredentials: new SigningCredentials(_key, SecurityAlgorithms.HmacSha256)
        );

    /// <summary>
    /// הגדרת הפרמטרים שבאמצעותם השרת יבדוק אם טוקן שמגיע מהלקוח הוא תקין ומקורי
    /// </summary>
    public static TokenValidationParameters GetTokenValidationParameters() =>
        new TokenValidationParameters
        {
            ValidIssuer = _issuer,
            ValidAudience = _issuer,
            IssuerSigningKey = _key,
            
            // הגדרת סוגי ה-Claims עבור תפקידים ושמות כדי שתכונת ה-[Authorize] תעבוד
            RoleClaimType = ClaimTypes.Role,
            NameClaimType = ClaimTypes.Name,
            
            // ביטול השהיית הזמן כדי שהטוקן יפוג בדיוק בזמן שהוגדר לו
            ClockSkew = TimeSpan.Zero 
        };

    /// <summary>
    /// המרת אובייקט הטוקן למחרוזת טקסט מוצפנת שניתן לשלוח ללקוח
    /// </summary>
    public static string WriteToken(SecurityToken token) =>
        new JwtSecurityTokenHandler().WriteToken(token);
}