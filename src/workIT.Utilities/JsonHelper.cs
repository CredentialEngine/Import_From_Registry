using System;
using System.Web.Mvc;

namespace workIT.Utilities
{
    public static class JsonHelper
    {
        /// <summary>
        /// Get a JSONResult from an input object. Will return with JsonRequestBehavior set to AllowGet and MaxJsonLength set to Int32.MaxValue.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static JsonResult GetRawJson( object input )
        {
            var result = new JsonResult();
            result.Data = input;
            result.JsonRequestBehavior = JsonRequestBehavior.AllowGet;
            result.MaxJsonLength = Int32.MaxValue;
            return result;
        }

        /// <summary>
        /// Get a JSONResult from an input object with wrapper to help with client-side error handling.
        /// </summary>
        /// <param name="input"></param>
        /// <param name="valid"></param>
        /// <param name="status"></param>
        /// <param name="extra"></param>
        /// <returns></returns>
        public static JsonResult GetJsonWithWrapper( object input, bool valid, string status, object extra )
        {
            var data = new
            {
                data = input,
                valid = valid,
                status = status,
                extra = extra
            };
            return GetRawJson( data );
        }
        public static JsonResult GetJsonWithWrapper( object input )
        {
            return GetJsonWithWrapper( input, true, "okay", null );
        }

    }
}