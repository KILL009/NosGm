using Frostvein.Data;

namespace Frostvein.DAL.Interface
{
    public interface IMultiAccountExceptionDAO
    {
        #region Methods

        MultiAccountExceptionDTO Insert(MultiAccountExceptionDTO exception);

        MultiAccountExceptionDTO LoadByAccount(long accountId);

        #endregion
    }
}
