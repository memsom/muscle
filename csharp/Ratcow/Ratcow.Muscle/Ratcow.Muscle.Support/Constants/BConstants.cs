namespace Ratcow.Muscle.Support.Constants
{
    public static class BConstants
    {
        public static int B_ERROR = -1;   /**< A value typically returned by a function or method with return type status_t, to indicate that it failed.  (When checking the value, it's better to check against B_NO_ERROR though, in case other failure values are defined in the future) */
        public static int B_NO_ERROR = 0; /**< The value returned by a function or method with return type status_t, to indicate that it succeeded with no errors. */
        public static int B_OK = 0;       /**< Synonym for B_NO_ERROR */
    }
}
