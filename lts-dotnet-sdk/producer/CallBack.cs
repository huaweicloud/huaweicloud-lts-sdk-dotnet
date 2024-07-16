using HuaweiCloud.SDK.LTS.HTTP;

namespace HuaweiCloud.SDK.LTS
{
    public abstract class CallBack
    {
        public abstract void Success(Result result);
        public abstract void Fail(Result result);
    }
}