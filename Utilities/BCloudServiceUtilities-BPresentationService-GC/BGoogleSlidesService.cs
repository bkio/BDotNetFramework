/// MIT License, Copyright Burak Kara, burak@burak.io, https://en.wikipedia.org/wiki/MIT_License

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Requests;
using Google.Apis.Services;
using Google.Apis.Slides.v1;
using Google.Apis.Slides.v1.Data;
using BCommonUtilities;

namespace BCloudServiceUtilities
{
    public class BGoogleSlidesService
    {
        /// <summary>
        /// <para>Holds initialization success</para>
        /// </summary>
        private readonly bool bInitializationSucceed;

        private readonly string ProgramUniqueID;

        private readonly string ProjectID;

        private readonly ServiceAccountCredential Credential;

        /// <summary>
        /// 
        /// <para>BGoogleSlidesService: Parametered Constructor</para>
        /// 
        /// <para>Parameters:</para>
        /// <para><paramref name="_ProgramUniqueID"/>           Program Unique ID</para>
        /// <para><paramref name="_ProjectID"/>                 GC Project ID</para>
        /// <para><paramref name="_ErrorMessageAction"/>        Error messages will be pushed to this action</para>
        /// 
        /// </summary>
        public BGoogleSlidesService(
            string _ProgramUniqueID,
            string _ProjectID,
            Action<string> _ErrorMessageAction = null)
        {
            ProgramUniqueID = _ProgramUniqueID;
            ProjectID = _ProjectID;
            try
            {
                string ApplicationCredentials = Environment.GetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS");
                string ApplicationCredentialsPlain = Environment.GetEnvironmentVariable("GOOGLE_PLAIN_CREDENTIALS");

                if (ApplicationCredentials == null && ApplicationCredentialsPlain == null)
                {
                    _ErrorMessageAction?.Invoke("BGoogleSlidesService->Constructor: GOOGLE_APPLICATION_CREDENTIALS (or GOOGLE_PLAIN_CREDENTIALS) environment variable is not defined.");
                    bInitializationSucceed = false;
                }
                else
                {
                    if (ApplicationCredentials == null)
                    {
                        if (!BUtility.HexDecode(out ApplicationCredentialsPlain, ApplicationCredentialsPlain, _ErrorMessageAction))
                        {
                            throw new Exception("Hex decode operation for application credentials plain has failed.");
                        }
                        Credential = GoogleCredential.FromJson(ApplicationCredentialsPlain)
                                         .CreateScoped(
                                            new string[]
                                            {
                                                SlidesService.Scope.PresentationsReadonly
                                            })
                                         .UnderlyingCredential as ServiceAccountCredential;
                    }
                    else
                    {
                        using (var Stream = new FileStream(ApplicationCredentials, FileMode.Open, FileAccess.Read))
                        {
                            Credential = GoogleCredential.FromStream(Stream)
                                         .CreateScoped(
                                            new string[]
                                            {
                                                SlidesService.Scope.PresentationsReadonly
                                            })
                                         .UnderlyingCredential as ServiceAccountCredential;
                        }
                    }

                    if (Credential != null)
                    {
                        bInitializationSucceed = true;
                    }
                    else
                    {
                        bInitializationSucceed = false;
                    }
                }
            }
            catch (Exception e)
            {
                _ErrorMessageAction?.Invoke("BGoogleSlidesService->Constructor: " + e.Message + ", Trace: " + e.StackTrace);
                bInitializationSucceed = false;
            }
        }

        private SlidesService GetService()
        {
            return new SlidesService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = Credential,
                ApplicationName = ProgramUniqueID
            });
        }

        /// <summary>
        ///
        /// <para>HasInitializationSucceed:</para>
        /// 
        /// <returns>     Returns: Initialization succeed or failed </returns>
        ///
        /// </summary>
        public bool HasInitializationSucceed()
        {
            return bInitializationSucceed;
        }

        public enum EBGetPresentationResult
        {
            Success,
            NotFound,
            Unauthorized,
            InternalError
        };

        public EBGetPresentationResult GetPagesAsThumbnails(out List<string> _ThumbnailUrls, string _GoogleSlidesID, Action<string> _ErrorMessageAction = null)
        {
            _ThumbnailUrls = new List<string>();

            try
            {
                using (var Service = GetService())
                {
                    var Requests = new BatchRequest(Service);

                    var GetRequest = Service.Presentations.Get(_GoogleSlidesID);
                    if (GetRequest != null)
                    {
                        var PresentationResult = GetRequest.Execute();
                        if (PresentationResult.Slides != null)
                        {
                            var ProgressStack = new ConcurrentStack<object>();
                            bool bSuccess = true;

                            var SortedUrls = new SortedDictionary<int, string>();

                            foreach (var CurrentSlide in PresentationResult.Slides)
                            {
                                if (CurrentSlide != null)
                                {
                                    Requests.Queue<Thumbnail>(Service.Presentations.Pages.GetThumbnail(_GoogleSlidesID, CurrentSlide.ObjectId),
                                    (Content, Error, i, Message) =>
                                    {
                                        if (!bSuccess) return;

                                        if (Error != null)
                                        {
                                            _ErrorMessageAction?.Invoke("BGoogleSlidesService->GetPagesAsThumbnails->Error: " + Error.Message);
                                            ProgressStack.Clear();
                                            bSuccess = false;
                                        }
                                        else
                                        {
                                            ProgressStack.TryPop(out object Ignore);

                                            SortedUrls.Add(i, Content.ContentUrl);
                                        }
                                    });
                                    ProgressStack.Push(new object());
                                }
                            }

                            if (ProgressStack.Count > 0)
                            {
                                try
                                {
                                    using (var CreatedTask = Requests.ExecuteAsync())
                                    {
                                        CreatedTask.Wait();
                                    }
                                }
                                catch (Exception e)
                                {
                                    _ErrorMessageAction?.Invoke("BGoogleSlidesService->GetPagesAsThumbnails->Exception: " + e.Message);
                                    bSuccess = false;
                                }

                                while (bSuccess && ProgressStack.Count > 0)
                                {
                                    Thread.Sleep(250);
                                }

                                if (!bSuccess)
                                {
                                    return EBGetPresentationResult.InternalError;
                                }

                                foreach (var CurrentPair in SortedUrls)
                                {
                                    _ThumbnailUrls.Add(CurrentPair.Value);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                _ErrorMessageAction?.Invoke("BGoogleSlidesService->GetPagesAsThumbnails: " + e.Message + ", Trace: " + e.StackTrace);

                if (e is Google.GoogleApiException)
                {
                    var Casted = e as Google.GoogleApiException;
                    if (Casted.Error.Code == 400 || Casted.Error.Code == 404)
                    {
                        return EBGetPresentationResult.NotFound;
                    }
                    if (Casted.Error.Code == 403)
                    {
                        return EBGetPresentationResult.Unauthorized;
                    }
                }
                return EBGetPresentationResult.InternalError;
            }
            return EBGetPresentationResult.Success;
        }

        public enum EBGetSpeakerNotesResult
        {
            Success,
            NotFound,
            SlideIDOutOfBounds,
            SlideDoesNotHaveNotes,
            Unauthorized,
            InternalError
        };

        public EBGetSpeakerNotesResult GetSpeakerNotes(out string _SpeakerNotes, string _GoogleSlidesID, int _SlideID, Action<string> _ErrorMessageAction)
        {
            _SpeakerNotes = "";

            try
            {
                using (var Service = GetService())
                {
                    var Requests = new BatchRequest(Service);

                    var GetRequest = Service.Presentations.Get(_GoogleSlidesID);
                    if (GetRequest != null)
                    {
                        var PresentationResult = GetRequest.Execute();
                        if (PresentationResult.Slides != null)
                        {
                            if (PresentationResult.Slides.Count <= _SlideID)
                            {
                                return EBGetSpeakerNotesResult.SlideIDOutOfBounds;
                            }

                            var SlidePage = PresentationResult.Slides[_SlideID];
                            if (SlidePage != null)
                            {
                                var SlideProperties = SlidePage.SlideProperties;
                                if (SlideProperties != null && SlideProperties.NotesPage != null && SlideProperties.NotesPage.PageElements != null)
                                {
                                    foreach (var Element in SlideProperties.NotesPage.PageElements)
                                    {
                                        if (Element != null && Element.Shape != null && Element.Shape.Text != null && Element.Shape.Text.TextElements != null)
                                        {
                                            foreach (var CurrentText in Element.Shape.Text.TextElements)
                                            {
                                                if (CurrentText != null && CurrentText.TextRun != null && CurrentText.TextRun.Content != null)
                                                {
                                                    _SpeakerNotes += CurrentText.TextRun.Content + "\n";
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                _ErrorMessageAction?.Invoke("BGoogleSlidesService->GetSpeakerNotes: " + e.Message + ", Trace: " + e.StackTrace);

                if (e is Google.GoogleApiException)
                {
                    var Casted = e as Google.GoogleApiException;
                    if (Casted.Error.Code == 400 || Casted.Error.Code == 404)
                    {
                        return EBGetSpeakerNotesResult.NotFound;
                    }
                    if (Casted.Error.Code == 403)
                    {
                        return EBGetSpeakerNotesResult.Unauthorized;
                    }
                }
                return EBGetSpeakerNotesResult.InternalError;
            }
            if (_SpeakerNotes.Length == 0)
            {
                return EBGetSpeakerNotesResult.SlideDoesNotHaveNotes;
            }
            return EBGetSpeakerNotesResult.Success;
        }
    }
}