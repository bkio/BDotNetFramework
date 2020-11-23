/// MIT License, Copyright Burak Kara, burak@burak.io, https://en.wikipedia.org/wiki/MIT_License

using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace ServiceUtilities
{
    ///<summary>
    ///
    /// When there is a new Action;
    /// 
    /// 1) Change microservice-dependency-map.cs also, if action is designed to invoke a service by pubsub and by a storage object change; change InvokedByStoragePubSubActions; otherwise InvokedByRegularPubSubActions
    /// 2) Search relevant PubSub_Part{No} in all terraform scripts; add the action in the correct places
    ///
    /// PubSub_Part1: Always
    /// PubSub_Part2: If action is designed to invoke a service by pubsub and by a storage object change 
    ///               (if service does not exist there yet; create the relevant part in the service terraform script)
    /// PubSub_Part3: If action is designed to invoke a service by pubsub (If Part2 is relevant; this must be relevant too)
    /// PubSub_Part4: If action is designed to invoke a service by pubsub (If Part2 is relevant; this must be relevant too)
    ///               (if service does not exist there yet; create the relevant part in the service terraform script);
    ///               (sometimes awaken services need to listen an action topic, so an action does not always have to invoke a service)
    ///               
    ///</summary>
    public static class Actions
    {
        public enum EAction
        {
            NONE,
            ACTION_OPERATION_TIMEOUT,
            ACTION_USER_CREATED,
            ACTION_USER_DELETED,
            ACTION_USER_UPDATED,
            ACTION_AUTH_SERVICE_DELIVERY_ENSURER,
            ACTION_CAD_FILE_SERVICE_DELIVERY_ENSURER,
            ACTION_MODEL_CREATED,
            ACTION_MODEL_DELETED,
            ACTION_MODEL_UPDATED,
            ACTION_MODEL_SHARED_WITH_USER_IDS_CHANGED,
            ACTION_MODEL_REVISION_CREATED,
            ACTION_MODEL_REVISION_DELETED,
            ACTION_MODEL_REVISION_UPDATED,
            ACTION_MODEL_REVISION_FILE_ENTRY_UPDATED,
            ACTION_MODEL_REVISION_FILE_ENTRY_DELETED,
            ACTION_MODEL_REVISION_FILE_ENTRY_DELETE_ALL,
            ACTION_MODEL_REVISION_RAW_FILE_UPLOADED,
            ACTION_STORAGE_FILE_UPLOADED,
            ACTION_STORAGE_FILE_DELETED,
            ACTION_ZIP_AND_UPLOAD_MODELS_TO_CDF,
            ACTION_BATCH_PROCESS_FAILED
        }

        //Deployment name and build number are appended to prefix by Manager_PubSubService;
        //Therefore this should only be called by Manager_PubSubService.SetDeploymentBranchNameAndBuildNumber() (Except microservice-dependency-map.cs for local debug run)
        public static readonly Dictionary<EAction, string> ActionStringPrefixMap = new Dictionary<EAction, string>()
        {
            [EAction.ACTION_OPERATION_TIMEOUT] = "action_operation_timeout_",
            [EAction.ACTION_USER_CREATED] = "action_user_created_",
            [EAction.ACTION_USER_DELETED] = "action_user_deleted_",
            [EAction.ACTION_USER_UPDATED] = "action_user_updated_",
            [EAction.ACTION_AUTH_SERVICE_DELIVERY_ENSURER] = "action_auth_service_delivery_ensurer_",
            [EAction.ACTION_CAD_FILE_SERVICE_DELIVERY_ENSURER] = "action_cad_file_service_delivery_ensurer_",
            [EAction.ACTION_MODEL_CREATED] = "action_model_created_",
            [EAction.ACTION_MODEL_DELETED] = "action_model_deleted_",
            [EAction.ACTION_MODEL_UPDATED] = "action_model_updated_",
            [EAction.ACTION_MODEL_SHARED_WITH_USER_IDS_CHANGED] = "action_model_shared_with_user_ids_changed_",
            [EAction.ACTION_MODEL_REVISION_CREATED] = "action_model_revision_created_",
            [EAction.ACTION_MODEL_REVISION_DELETED] = "action_model_revision_deleted_",
            [EAction.ACTION_MODEL_REVISION_UPDATED] = "action_model_revision_updated_",
            [EAction.ACTION_MODEL_REVISION_FILE_ENTRY_UPDATED] = "action_model_revision_file_entry_updated_",
            [EAction.ACTION_MODEL_REVISION_FILE_ENTRY_DELETED] = "action_model_revision_file_entry_deleted_",
            [EAction.ACTION_MODEL_REVISION_FILE_ENTRY_DELETE_ALL] = "action_model_revision_file_entry_delete_all_",
            [EAction.ACTION_MODEL_REVISION_RAW_FILE_UPLOADED] = "action_model_revision_raw_file_uploaded_",
            [EAction.ACTION_STORAGE_FILE_UPLOADED] = "action_storage_file_uploaded_",
            [EAction.ACTION_STORAGE_FILE_DELETED] = "action_storage_file_deleted_",
            [EAction.ACTION_ZIP_AND_UPLOAD_MODELS_TO_CDF] = "action_zip_and_upload_models_to_cdf_",
            [EAction.ACTION_BATCH_PROCESS_FAILED] = "action_cad_process_batch_process_failed_"
        };

        public static Action DeserializeAction(EAction _IdentifiedAction, string _SerializedAction)
        {
            switch (_IdentifiedAction)
            {
                case EAction.ACTION_OPERATION_TIMEOUT:
                    return JsonConvert.DeserializeObject<Action_OperationTimeout>(_SerializedAction);
                case EAction.ACTION_USER_CREATED:
                    return JsonConvert.DeserializeObject<Action_UserCreated>(_SerializedAction);
                case EAction.ACTION_USER_DELETED:
                    return JsonConvert.DeserializeObject<Action_UserDeleted>(_SerializedAction);
                case EAction.ACTION_USER_UPDATED:
                    return JsonConvert.DeserializeObject<Action_UserUpdated>(_SerializedAction);
                case EAction.ACTION_AUTH_SERVICE_DELIVERY_ENSURER:
                case EAction.ACTION_CAD_FILE_SERVICE_DELIVERY_ENSURER:
                    return (Action)Type.GetType("ServiceUtilities.Action_DeliveryEnsurer")?.GetMethod("DeserializeDeliveryEnsurerAction")?.Invoke(null, new object[] { _SerializedAction });
                case EAction.ACTION_MODEL_CREATED:
                    return JsonConvert.DeserializeObject<Action_ModelCreated>(_SerializedAction);
                case EAction.ACTION_MODEL_DELETED:
                    return JsonConvert.DeserializeObject<Action_ModelDeleted>(_SerializedAction);
                case EAction.ACTION_MODEL_UPDATED:
                    return JsonConvert.DeserializeObject<Action_ModelUpdated>(_SerializedAction);
                case EAction.ACTION_MODEL_SHARED_WITH_USER_IDS_CHANGED:
                    return JsonConvert.DeserializeObject<Action_ModelSharedWithUserIdsChanged>(_SerializedAction);
                case EAction.ACTION_MODEL_REVISION_CREATED:
                    return JsonConvert.DeserializeObject<Action_ModelRevisionCreated>(_SerializedAction);
                case EAction.ACTION_MODEL_REVISION_DELETED:
                    return JsonConvert.DeserializeObject<Action_ModelRevisionDeleted>(_SerializedAction);
                case EAction.ACTION_MODEL_REVISION_UPDATED:
                    return JsonConvert.DeserializeObject<Action_ModelRevisionUpdated>(_SerializedAction);
                case EAction.ACTION_MODEL_REVISION_FILE_ENTRY_DELETED:
                    return JsonConvert.DeserializeObject<Action_ModelRevisionFileEntryDeleted>(_SerializedAction);
                case EAction.ACTION_MODEL_REVISION_FILE_ENTRY_UPDATED:
                    return JsonConvert.DeserializeObject<Action_ModelRevisionFileEntryUpdated>(_SerializedAction);
                case EAction.ACTION_MODEL_REVISION_FILE_ENTRY_DELETE_ALL:
                    return JsonConvert.DeserializeObject<Action_ModelRevisionFileEntryDeleteAll>(_SerializedAction);
                case EAction.ACTION_MODEL_REVISION_RAW_FILE_UPLOADED:
                    return JsonConvert.DeserializeObject<Action_ModelRevisionRawFileUploaded>(_SerializedAction);
                case EAction.ACTION_STORAGE_FILE_UPLOADED:
                    return JsonConvert.DeserializeObject<Action_StorageFileUploaded>(_SerializedAction);
                case EAction.ACTION_STORAGE_FILE_DELETED:
                    return JsonConvert.DeserializeObject<Action_StorageFileDeleted>(_SerializedAction);
                case EAction.ACTION_ZIP_AND_UPLOAD_MODELS_TO_CDF:
                    return JsonConvert.DeserializeObject<Action_ZipAndUploadModelsToCdf>(_SerializedAction);
                case EAction.ACTION_BATCH_PROCESS_FAILED:
                    return JsonConvert.DeserializeObject<Action_BatchProcessFailed>(_SerializedAction);
                default:
                    break;
            }
            return null;
        }
    }
}