using NosGm.Data;

namespace NosGm.DAL.Interface
{
    public interface IMultiAccountExceptionDAO
    {
        #region Methods

        MultiAccountExceptionDTO Insert(MultiAccountExceptionDTO exception);

        MultiAccountExceptionDTO LoadByAccount(long accountId);

        #endregion
    }
}
