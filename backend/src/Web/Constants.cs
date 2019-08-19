﻿namespace EventManagement.WebApp
{
    public static class Constants
    {
        /// <summary>
        /// Name of the authentication scheme to authenticate with a
        /// qr code scanner app by scanning a Master QR Code.
        /// </summary>
        public const string MasterQrCodeAuthenticationScheme = "masterqr";

        /// <summary>
        /// Policy to grant access to the Event Management API.
        /// </summary>
        public const string EventManagementApiPolicy = "EventManagementCookieOrAccessToken";
    }
}
