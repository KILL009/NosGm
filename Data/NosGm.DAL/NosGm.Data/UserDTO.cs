using System;

namespace NosGm.Data
{
    [Serializable]
    public class UserDTO
    {
        #region Properties

        public string Name { get; set; }

        public string Password { get; set; }

        public string Unknown { get; set; }

        #endregion
    }
}