using System;
using System.Web;
using Web_perfume.Models;

namespace Web_perfume.Helpers
{
    public static class AuthHelper
    {
        private const string UserSessionKey = "PERFUME_USER_SESSION";

        public static void SetUser(HttpSessionStateBase session, User user)
        {
            if (session != null && user != null)
            {
                session[UserSessionKey] = new UserSessionModel
                {
                    UserID = user.UserID,
                    FullName = user.FullName,
                    Email = user.Email,
                    RoleID = user.RoleID,
                    RoleName = user.Role != null ? user.Role.RoleName : (user.RoleID == 1 ? "Admin" : "Customer")
                };
            }
        }

        public static UserSessionModel GetUser(HttpSessionStateBase session)
        {
            if (session == null) return null;
            return session[UserSessionKey] as UserSessionModel;
        }

        public static bool IsLoggedIn(HttpSessionStateBase session)
        {
            return GetUser(session) != null;
        }

        public static bool IsAdmin(HttpSessionStateBase session)
        {
            var user = GetUser(session);
            return user != null && string.Equals(user.RoleName, "Admin", StringComparison.OrdinalIgnoreCase);
        }

        public static void Logout(HttpSessionStateBase session)
        {
            if (session != null)
            {
                session.Remove(UserSessionKey);
            }
        }
    }

    public class UserSessionModel
    {
        public int UserID { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public int RoleID { get; set; }
        public string RoleName { get; set; }

        public bool IsAdmin => string.Equals(RoleName, "Admin", StringComparison.OrdinalIgnoreCase);
    }
}

