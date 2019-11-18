/// MIT License, Copyright Burak Kara, burak@burak.io, https://en.wikipedia.org/wiki/MIT_License

using System;
using System.Collections.Generic;
using BCloudServiceUtilities;

namespace BCloudServiceUtilitiesTest.Tests
{
    public class BEmailServicesTest
    {
        private readonly IBMailServiceInterface SelectedMailService;

        private readonly Action<string> PrintAction;

        public BEmailServicesTest(IBMailServiceInterface _SelectedMailService, Action<string> _PrintAction)
        {
            SelectedMailService = _SelectedMailService;

            PrintAction = _PrintAction;
        }

        public bool Start()
        {
            PrintAction?.Invoke("BEmailServicesTest->Info-> Test is starting.");

            if (SelectedMailService == null)
            {
                PrintAction?.Invoke("BEmailServicesTest->Error-> Given SelectedMailService is null.");
                return false;
            }

            if (!SelectedMailService.HasInitializationSucceed())
            {
                PrintAction?.Invoke("BEmailServicesTest->Error-> Initialization failed.");
                return false;
            }
            PrintAction?.Invoke("BEmailServicesTest->Log-> Initialization succeed.");

            if (!SelectedMailService.SendEmails(new List<BMailServiceMailStruct>
            {
                new BMailServiceMailStruct(new BMailServicerReceiverStruct("burak@burak.io", "Burak Kara"), "Test e-mail - 1", "Burak.IO Test E-mail - 1", "<strong>Burak.IO Test E-mail with strong html wrapper - 1</strong>"),
                new BMailServiceMailStruct(new BMailServicerReceiverStruct("burak.io.mail@gmail.com", "Burak Kara G-mail - 2"), "Test e-mail", "Burak.IO Test E-mail - 2", "<strong>Burak.IO Test E-mail with strong html wrapper - 2</strong>")
            },
            Console.WriteLine)) return false;

            if (!SelectedMailService.BroadcastEmail(new List<BMailServicerReceiverStruct>
            {
                new BMailServicerReceiverStruct("burak@burak.io", "Burak Kara"),
                new BMailServicerReceiverStruct("burak.io.mail@gmail.com", "Burak Kara G-mail - 2")
            },
            "Test broadcast e-mail",
            "Burak.IO broadcast test e-mail",
            "<strong>Burak.IO broadcast test e-mail with strong html wrapper</strong>",
            Console.WriteLine)) return false;

            return true;
        }
    }
}