/// MIT License, Copyright Burak Kara, burak@burak.io, https://en.wikipedia.org/wiki/MIT_License
 
using Newtonsoft.Json;

namespace ServiceUtilities
{
    public abstract class Action
    {
        public abstract Actions.EAction GetActionType();
        
        //Reminder for implementing static Action_[] DefaultInstance = new Action_[]();
        protected abstract Action GetStaticDefaultInstance();
    }
}