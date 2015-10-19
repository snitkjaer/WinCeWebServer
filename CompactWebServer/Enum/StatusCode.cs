//------------------------------------------------------------------------------
// StatusCode.cs
//
// http://bansky.net
//
// This code was written by Pavel Bansky. It is released under the terms of 
// the Creative Commons "Attribution NonCommercial ShareAlike 2.5" license.
// http://creativecommons.org/licenses/by-nc-sa/2.5/
//
//------------------------------------------------------------------------------
namespace CompactWebServer
{
    /// <summary>
    /// HTTPS StatusCodes
    /// </summary>
    public enum StatusCode { 
        /// <summary>
        /// 200 OK
        /// </summary>
        OK = 200, 
        /// <summary>
        /// 400 Bad Request
        /// </summary>
        BadRequest = 400, 
        /// <summary>
        /// 404 File not found
        /// </summary>
        NotFound = 404,
        /// <summary>
        /// 403 Access Forbidden
        /// </summary>
        Forbiden = 403,

        InternalServerError = 500
    };
}
