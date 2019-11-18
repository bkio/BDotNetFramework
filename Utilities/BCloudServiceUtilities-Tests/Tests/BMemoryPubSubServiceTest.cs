/// MIT License, Copyright Burak Kara, burak@burak.io, https://en.wikipedia.org/wiki/MIT_License

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using BCloudServiceUtilities;
using BCommonUtilities;
using Newtonsoft.Json.Linq;

namespace BCloudServiceUtilitiesTest.Tests
{
    public class BMemoryPubSubServiceTest
    {
        private readonly IBMemoryServiceInterface SelectedMemoryService;

        private readonly BMemoryQueryParameters MemoryQueryParameters;
        private readonly BMemoryQueryParameters ExpireTestMemoryQueryParameters;

        private readonly Action<string> PrintAction;

        public BMemoryPubSubServiceTest(IBMemoryServiceInterface _MemoryService, Action<string> _PrintAction)
        {
            SelectedMemoryService = _MemoryService;

            MemoryQueryParameters.Domain = "TestUsername";
            MemoryQueryParameters.SubDomain = "TestModelname";
            MemoryQueryParameters.Identifier = "1";

            ExpireTestMemoryQueryParameters.Domain = "ExpireTestUsername";
            ExpireTestMemoryQueryParameters.SubDomain = "ExpireTestModelname";
            ExpireTestMemoryQueryParameters.Identifier = "1";

            PrintAction = _PrintAction;
        }

        private void PreCleanup()
        {
            SelectedMemoryService.GetPubSubService().DeleteTopicGlobally(
                MemoryQueryParameters,
                null);

            SelectedMemoryService.GetPubSubService().DeleteTopicGlobally(
                ExpireTestMemoryQueryParameters,
                null);

            SelectedMemoryService.DeleteAllKeys(
                MemoryQueryParameters,
                true,
                null,
                false);

            SelectedMemoryService.DeleteAllKeys(
                ExpireTestMemoryQueryParameters,
                true,
                null,
                false);

            SelectedMemoryService.EmptyList(
                MemoryQueryParameters, 
                "TestList1", 
                true, 
                null,
                false);

            //To not receive subscription messages for actions above
            Thread.Sleep(1000);
        }

        private List<string> SubscribeMessages = new List<string>();
        private readonly BValue<int> SubscribeDoneCounter = new BValue<int>(0, EBProducerStatus.SingleProducer);
        private readonly int NumberOfExpectedSubscriptionMessages = 11;
        private readonly int MaxWaitSecondsForAllSubscriptionMessages = 10;
        private readonly BValue<bool> FailureStatus = new BValue<bool>(false, EBProducerStatus.MultipleProducer);

        public bool Start()
        {
            PrintAction?.Invoke("BMemoryPubSubServiceTest->Info-> Test is starting.");

            if (SelectedMemoryService == null)
            {
                PrintAction?.Invoke("BMemoryPubSubServiceTest->Error-> Given SelectedMemoryService is null.");
                return false;
            }

            if (!SelectedMemoryService.HasInitializationSucceed())
            {
                PrintAction?.Invoke("BMemoryPubSubServiceTest->Error-> Initialization failed.");
                return false;
            }

            if (SelectedMemoryService.GetPubSubService() == null)
            {
                PrintAction?.Invoke("BMemoryPubSubServiceTest->Error-> Given PubSubService is null.");
                return false;
            }

            SelectedMemoryService.GetPubSubService().EnsureUniqueMessageDelivery(SelectedMemoryService, PrintAction);

            PrintAction?.Invoke("BMemoryPubSubServiceTest->Log-> Initialization succeed.");

            PreCleanup();

            SelectedMemoryService.GetPubSubService().Subscribe(
                MemoryQueryParameters,
                (string Topic, JObject Message) =>
                {
                    lock (SubscribeDoneCounter.Monitor)
                    {
                        SubscribeMessages.Add("BMemoryPubSubServiceTest->Sub-> " + Topic + "-> " + Message.ToString());
                        SubscribeDoneCounter.Set(SubscribeDoneCounter.Get() + 1);
                    }
                },
                (string Message) =>
                {
                    Console.WriteLine("BMemoryPubSubServiceTest->Sub->Error-> " + Message);
                    FailureStatus.Set(true);
                });

            bool bLocalResult = SelectedMemoryService.SetKeyValue(
                MemoryQueryParameters,
                new Tuple<string, BPrimitiveType>[]
                {
                    new Tuple<string, BPrimitiveType>("TestKey1", new BPrimitiveType(123)),
                    new Tuple<string, BPrimitiveType>("TestKey2", new BPrimitiveType(123.00400422f)),
                    new Tuple<string, BPrimitiveType>("TestKey3", new BPrimitiveType("TestVal3"))
                },
                (string Message) =>
                {
                    bLocalResult = false;
                    Console.WriteLine("BMemoryPubSubServiceTest->SetKeyValue-1->Error-> " + Message);
                });
            if (!bLocalResult)
            {
                PrintAction?.Invoke("BMemoryPubSubServiceTest->SetKeyValue-1 has failed.");
                return false;
            }
            PrintAction?.Invoke("BMemoryPubSubServiceTest->SetKeyValue-1 has succeed.");

            var LocalResult_1 = SelectedMemoryService.GetAllKeyValues(
                MemoryQueryParameters,
                (string Message) => Console.WriteLine("BMemoryPubSubServiceTest->GetAllKeyValues->Error-> " + Message));
            if (LocalResult_1 == null || LocalResult_1.Length != 3)
            {
                PrintAction?.Invoke("BMemoryPubSubServiceTest->GetAllKeyValues-1 has failed. IsNull: " + (LocalResult_1 == null) + ", Length: " + LocalResult_1?.Length);
                return false;
            }
            foreach (var Tuple_1 in LocalResult_1)
            {
                PrintAction?.Invoke("BMemoryPubSubServiceTest->GetAllKeyValues-1->" + Tuple_1.Item1 + ": " + Tuple_1.Item2.ToString());
            }

            bLocalResult = SelectedMemoryService.DeleteAllKeys(
                MemoryQueryParameters,
                true,
                (string Message) =>
                {
                    bLocalResult = false;
                    Console.WriteLine("BMemoryPubSubServiceTest->DeleteAllKeys-1->Error-> " + Message);
                });
            if (!bLocalResult)
            {
                PrintAction?.Invoke("BMemoryPubSubServiceTest->DeleteAllKeys-1 has failed.");
                return false;
            }
            PrintAction?.Invoke("BMemoryPubSubServiceTest->DeleteAllKeys-1 has succeed.");

            bLocalResult = SelectedMemoryService.SetKeyValue(
                MemoryQueryParameters,
                new Tuple<string, BPrimitiveType>[]
                {
                    new Tuple<string, BPrimitiveType>("TestKey1", new BPrimitiveType(123)),
                    new Tuple<string, BPrimitiveType>("TestKey2", new BPrimitiveType(123.00400422f)),
                    new Tuple<string, BPrimitiveType>("TestKey3", new BPrimitiveType("TestVal3"))
                },
                (string Message) =>
                {
                    bLocalResult = false;
                    Console.WriteLine("BMemoryPubSubServiceTest->SetKeyValue-2->Error-> " + Message);
                });
            if (!bLocalResult)
            {
                PrintAction?.Invoke("BMemoryPubSubServiceTest->SetKeyValue-2 has failed.");
                return false;
            }
            PrintAction?.Invoke("BMemoryPubSubServiceTest->SetKeyValue-2 has succeed.");

            bLocalResult = true;
            long KeysCount = SelectedMemoryService.GetKeysCount(
                MemoryQueryParameters,
                (string Message) =>
                {
                    bLocalResult = false;
                    Console.WriteLine("BMemoryPubSubServiceTest->GetKeysCount-1->Error-> " + Message);
                });
            if (!bLocalResult)
            {
                PrintAction?.Invoke("BMemoryPubSubServiceTest->GetKeysCount-1 has failed.");
                return false;
            }
            if (KeysCount != 3)
            {
                PrintAction?.Invoke("BMemoryPubSubServiceTest->GetKeysCount-1 expected 3, but result is: " + KeysCount);
                return false;
            }
            PrintAction?.Invoke("BMemoryPubSubServiceTest->GetKeysCount-1 has succeed.");

            bLocalResult = true;
            SelectedMemoryService.IncrementKeyValues(
                MemoryQueryParameters,
                new Tuple<string, long>[]
                {
                    new Tuple<string, long>("TestKey1", 100)
                },
                (string Message) =>
                {
                    bLocalResult = false;
                    Console.WriteLine("BMemoryPubSubServiceTest->IncrementKeyValues-1->Error-> " + Message);
                });
            if (!bLocalResult)
            {
                PrintAction?.Invoke("BMemoryPubSubServiceTest->IncrementKeyValues-1 has failed.");
                return false;
            }
            PrintAction?.Invoke("BMemoryPubSubServiceTest->IncrementKeyValues-1 has succeed.");

            bLocalResult = true;
            var Expected1223 = SelectedMemoryService.IncrementKeyByValueAndGet(MemoryQueryParameters,
                new Tuple<string, long>("TestKey1", 1000),
                (string Message) =>
                {
                    bLocalResult = false;
                    Console.WriteLine("BMemoryPubSubServiceTest->IncrementKeyByValueAndGet-1->Error-> " + Message);
                });
            if (!bLocalResult)
            {
                PrintAction?.Invoke("BMemoryPubSubServiceTest->IncrementKeyByValueAndGet-1 has failed.");
                return false;
            }
            if (Expected1223 != 1223)
            {
                PrintAction?.Invoke("BMemoryPubSubServiceTest->IncrementKeyByValueAndGet-1 expected 1223, but result is: " + Expected1223);
                return false;
            }
            PrintAction?.Invoke("BMemoryPubSubServiceTest->IncrementKeyByValueAndGet-1 has succeed.");

            bLocalResult = SelectedMemoryService.PushToListHead(MemoryQueryParameters, "TestList1", new BPrimitiveType[]
            {
                new BPrimitiveType(123),
                new BPrimitiveType(234)
            },
            false,
                (string Message) =>
                {
                    bLocalResult = false;
                    Console.WriteLine("BMemoryPubSubServiceTest->PushToListHead-1->Error-> " + Message);
                });
            if (!bLocalResult)
            {
                PrintAction?.Invoke("BMemoryPubSubServiceTest->PushToListHead-1 has failed.");
                return false;
            }
            PrintAction?.Invoke("BMemoryPubSubServiceTest->PushToListHead-1 has succeed.");

            bLocalResult = SelectedMemoryService.PushToListTail(MemoryQueryParameters, "TestList1", new BPrimitiveType[]
            {
                new BPrimitiveType(345),
                new BPrimitiveType(456)
            },
            true,
                (string Message) =>
                {
                    bLocalResult = false;
                    Console.WriteLine("BMemoryPubSubServiceTest->PushToListTail-1->Error-> " + Message);
                });
            if (!bLocalResult)
            {
                PrintAction?.Invoke("BMemoryPubSubServiceTest->PushToListTail-1 has failed.");
                return false;
            }
            PrintAction?.Invoke("BMemoryPubSubServiceTest->PushToListTail-1 has succeed.");

            bLocalResult = true;
            var GetAllElementsOfListResult_1 = SelectedMemoryService.GetAllElementsOfList(MemoryQueryParameters, "TestList1",
                (string Message) =>
                {
                    bLocalResult = false;
                    Console.WriteLine("BMemoryPubSubServiceTest->GetAllElementsOfList-1->Error-> " + Message);
                });
            if (!bLocalResult)
            {
                PrintAction?.Invoke("BMemoryPubSubServiceTest->GetAllElementsOfList-1 has failed.");
                return false;
            }
            if (GetAllElementsOfListResult_1 == null || GetAllElementsOfListResult_1.Length != 4)
            {
                PrintAction?.Invoke("BMemoryPubSubServiceTest->GetAllElementsOfList-1 result is either null or length is unexpected. Length: " + GetAllElementsOfListResult_1?.Length);
                return false;
            }
            PrintAction?.Invoke("BMemoryPubSubServiceTest->GetAllElementsOfList-1 has succeed.");

            var PopFirstElementOfListResult_1 = SelectedMemoryService.PopFirstElementOfList(MemoryQueryParameters, "TestList1", 
                (string Message) =>
                {
                    bLocalResult = false;
                    Console.WriteLine("BMemoryPubSubServiceTest->PopFirstElementOfList-1->Error-> " + Message);
                });
            if (!bLocalResult || PopFirstElementOfListResult_1 == null)
            {
                PrintAction?.Invoke("BMemoryPubSubServiceTest->PopFirstElementOfList-1 has failed. PopFirstElementOfListResult_1 null status: " + (PopFirstElementOfListResult_1 == null));
                return false;
            }
            PrintAction?.Invoke("BMemoryPubSubServiceTest->PopFirstElementOfList-1 succeed. Result: " + PopFirstElementOfListResult_1.ToString());

            var PopLastElementOfListResult_1 = SelectedMemoryService.PopLastElementOfList(MemoryQueryParameters, "TestList1",
                (string Message) =>
                {
                    bLocalResult = false;
                    Console.WriteLine("BMemoryPubSubServiceTest->PopLastElementOfList-1->Error-> " + Message);
                });
            if (!bLocalResult || PopLastElementOfListResult_1 == null)
            {
                PrintAction?.Invoke("BMemoryPubSubServiceTest->PopLastElementOfList-1 has failed. PopLastElementOfList_1 null status: " + (PopLastElementOfListResult_1 == null));
                return false;
            }
            PrintAction?.Invoke("BMemoryPubSubServiceTest->PopLastElementOfList-1 succeed. Result: " + PopLastElementOfListResult_1.ToString());

            bLocalResult = true;
            var ListSizeResult_1 = SelectedMemoryService.ListSize(MemoryQueryParameters, "TestList1", 
                (string Message) =>
                {
                    bLocalResult = false;
                    Console.WriteLine("BMemoryPubSubServiceTest->ListSize-1->Error-> " + Message);
                });
            if (!bLocalResult)
            {
                PrintAction?.Invoke("BMemoryPubSubServiceTest->ListSize-1 has failed.");
                return false;
            }
            if (ListSizeResult_1 != 2)
            {
                PrintAction?.Invoke("BMemoryPubSubServiceTest->ListSize-1: Expected result is 2, but returned: " + ListSizeResult_1);
                return false;
            }
            PrintAction?.Invoke("BMemoryPubSubServiceTest->ListSize-1 has succeed.");

            bLocalResult = SelectedMemoryService.ListContains(MemoryQueryParameters, "TestList1", new BPrimitiveType(123),
                (string Message) =>
                {
                    bLocalResult = false;
                    Console.WriteLine("BMemoryPubSubServiceTest->ListContains-1->Error-> " + Message);
                });
            if (!bLocalResult)
            {
                PrintAction?.Invoke("BMemoryPubSubServiceTest->ListContains-1 has failed.");
                return false;
            }
            PrintAction?.Invoke("BMemoryPubSubServiceTest->ListContains-1 has succeed.");

            bLocalResult = SelectedMemoryService.EmptyList(MemoryQueryParameters, "TestList1", true,
                (string Message) =>
                {
                    bLocalResult = false;
                    Console.WriteLine("BMemoryPubSubServiceTest->EmptyList-1->Error-> " + Message);
                });
            if (!bLocalResult)
            {
                PrintAction?.Invoke("BMemoryPubSubServiceTest->EmptyList-1 has failed.");
                return false;
            }
            PrintAction?.Invoke("BMemoryPubSubServiceTest->EmptyList-1 has succeed.");

            bLocalResult = true;
            var ListSizeResult_2 = SelectedMemoryService.ListSize(MemoryQueryParameters, "TestList1",
                (string Message) =>
                {
                    bLocalResult = false;
                    Console.WriteLine("BMemoryPubSubServiceTest->ListSize-2->Error-> " + Message);
                });
            if (!bLocalResult)
            {
                PrintAction?.Invoke("BMemoryPubSubServiceTest->ListSize-2 has failed.");
                return false;
            }
            if (ListSizeResult_2 != 0)
            {
                PrintAction?.Invoke("BMemoryPubSubServiceTest->ListSize-2: Expected result is 0, but returned: " + ListSizeResult_1);
                return false;
            }
            PrintAction?.Invoke("BMemoryPubSubServiceTest->ListSize-2 has succeed.");

            bLocalResult = true;
            var Expected1223Value = SelectedMemoryService.GetKeyValue(
                MemoryQueryParameters,
                "TestKey1",
                (string Message) =>
                {
                    bLocalResult = false;
                    Console.WriteLine("BMemoryPubSubServiceTest->GetKeyValue-1->Error-> " + Message);
                });
            if (!bLocalResult)
            {
                PrintAction?.Invoke("BMemoryPubSubServiceTest->GetKeyValue-1 has failed.");
                return false;
            }
            if (Expected1223Value == null || Expected1223Value.Type != EBPrimitiveTypeEnum.Integer || Expected1223Value.AsInteger != 1223)
            {
                PrintAction?.Invoke("BMemoryPubSubServiceTest->GetKeyValue-1 expected 1223, but result is: " + Expected1223Value?.ToString());
                return false;
            }
            PrintAction?.Invoke("BMemoryPubSubServiceTest->GetKeyValue-1 has succeed.");

            bLocalResult = SelectedMemoryService.DeleteKey(
                MemoryQueryParameters,
                "TestKey3",
                (string Message) =>
                {
                    bLocalResult = false;
                    Console.WriteLine("BMemoryPubSubServiceTest->DeleteKey-1->Error-> " + Message);
                });
            if (!bLocalResult)
            {
                PrintAction?.Invoke("BMemoryPubSubServiceTest->DeleteKey-1 has failed.");
                return false;
            }
            PrintAction?.Invoke("BMemoryPubSubServiceTest->DeleteKey-1 has succeed.");

            bLocalResult = SelectedMemoryService.DeleteKey(
                MemoryQueryParameters,
                "TestKeyNonExistent",
                (string Message) =>
                {
                    bLocalResult = false;
                    Console.WriteLine("BMemoryPubSubServiceTest->DeleteKey-2->Error-> Expected: " + Message);
                });
            if (bLocalResult)
            {
                PrintAction?.Invoke("BMemoryPubSubServiceTest->DeleteKey-2 did not fail.");
                return false;
            }
            PrintAction?.Invoke("BMemoryPubSubServiceTest->DeleteKey-2 has successfully failed.");

            bLocalResult = true;
            KeysCount = SelectedMemoryService.GetKeysCount(
                MemoryQueryParameters,
                (string Message) =>
                {
                    bLocalResult = false;
                    Console.WriteLine("BMemoryPubSubServiceTest->GetKeysCount-2->Error-> " + Message);
                });
            if (!bLocalResult)
            {
                PrintAction?.Invoke("BMemoryPubSubServiceTest->GetKeysCount-2 has failed.");
                return false;
            }
            if (KeysCount != 2)
            {
                PrintAction?.Invoke("BMemoryPubSubServiceTest->GetKeysCount-2 expected 2, but result is: " + KeysCount);
                return false;
            }
            PrintAction?.Invoke("BMemoryPubSubServiceTest->GetKeysCount-2 has succeed.");

            bLocalResult = true;
            var GetKeysResult = SelectedMemoryService.GetKeys(MemoryQueryParameters,
                (string Message) =>
                {
                    bLocalResult = false;
                    Console.WriteLine("BMemoryPubSubServiceTest->GetKeys-1->Error-> " + Message);
                });
            if (!bLocalResult || GetKeysResult == null || GetKeysResult.Length != 2)
            {
                PrintAction?.Invoke("BMemoryPubSubServiceTest->GetKeys-1 returned " + GetKeysResult?.Length + " results, but expected 2. Error status: " + bLocalResult);
                return false;
            }
            PrintAction?.Invoke("BMemoryPubSubServiceTest->GetKeys-1 has succeed.");

            bLocalResult = true;
            var GetKeysValuesResult = SelectedMemoryService.GetKeysValues(MemoryQueryParameters, new List<string>(GetKeysResult),
                (string Message) =>
                {
                    bLocalResult = false;
                    Console.WriteLine("BMemoryPubSubServiceTest->GetKeysValues-1->Error-> " + Message);
                });
            if (!bLocalResult || GetKeysValuesResult == null || GetKeysValuesResult.Count != 2)
            {
                PrintAction?.Invoke("BMemoryPubSubServiceTest->GetKeysValues-1 returned " + GetKeysValuesResult?.Count + " results, but expected 2. Error status: " + bLocalResult);
                return false;
            }
            PrintAction?.Invoke("BMemoryPubSubServiceTest->GetKeysValues-1 has succeed.");

            int ElapsedSeconds = 0;
            while (SubscribeDoneCounter.Get() != NumberOfExpectedSubscriptionMessages && ElapsedSeconds < MaxWaitSecondsForAllSubscriptionMessages && !FailureStatus.Get())
            {
                Thread.Sleep(1000);
                ElapsedSeconds++;
            }
            if (FailureStatus.Get())
            {
                PrintAction?.Invoke("Failure detected. Test failed.");
                return false;
            }
            if (SubscribeDoneCounter.Get() != NumberOfExpectedSubscriptionMessages)
            {
                PrintAction?.Invoke("Subscription messages timed out or processed multiple time.");
                return false;
            }
            lock (SubscribeDoneCounter.Monitor)
            {
                SubscribeMessages = SubscribeMessages.OrderBy(q => q).ToList();
                foreach (var Message in SubscribeMessages)
                {
                    PrintAction?.Invoke("Received message: " + Message);
                }
            }

            bLocalResult = true;
            SelectedMemoryService.GetPubSubService().DeleteTopicGlobally(
                MemoryQueryParameters,
                (string Message) =>
                {
                    bLocalResult = false;
                    Console.WriteLine("BMemoryPubSubServiceTest->DeleteTopicGlobally-1->Error-> " + Message);
                });
            if (!bLocalResult)
            {
                PrintAction?.Invoke("BMemoryPubSubServiceTest->DeleteTopicGlobally-1 has failed.");
                return false;
            }
            PrintAction?.Invoke("BMemoryPubSubServiceTest->DeleteTopicGlobally-1 has succeed.");

            bLocalResult = SelectedMemoryService.SetKeyValue(ExpireTestMemoryQueryParameters, new Tuple<string, BPrimitiveType>[]
            {
                new Tuple<string, BPrimitiveType>("TestKey1", new BPrimitiveType("TestValue1"))
            },
            (string Message) =>
            {
                bLocalResult = false;
                Console.WriteLine("BMemoryPubSubServiceTest->SetKeyValue-ExpireTest->Error-> " + Message);
            });
            if (!bLocalResult)
            {
                PrintAction?.Invoke("BMemoryPubSubServiceTest->SetKeyValue-ExpireTest has failed.");
                return false;
            }
            PrintAction?.Invoke("BMemoryPubSubServiceTest->SetKeyValue-ExpireTest has succeed.");

            bLocalResult = SelectedMemoryService.GetKeyExpireTime(ExpireTestMemoryQueryParameters, out TimeSpan TTL_1,
            (string Message) =>
            {
                bLocalResult = false;
                Console.WriteLine("BMemoryPubSubServiceTest->GetKeyExpireTime-ExpireTest-1->Error-> Expected: " + Message);
            });
            if (bLocalResult)
            {
                PrintAction?.Invoke("BMemoryPubSubServiceTest->GetKeyExpireTime-ExpireTest-1 did not fail.");
                return false;
            }
            PrintAction?.Invoke("BMemoryPubSubServiceTest->GetKeyExpireTime-ExpireTest-1 successfully failed.");

            bLocalResult = SelectedMemoryService.SetKeyExpireTime(ExpireTestMemoryQueryParameters, TimeSpan.FromSeconds(2.0f),
                (string Message) =>
                {
                    bLocalResult = false;
                    Console.WriteLine("BMemoryPubSubServiceTest->SetKeyExpireTime-ExpireTest-1->Error-> " + Message);
                });
            if (!bLocalResult)
            {
                PrintAction?.Invoke("BMemoryPubSubServiceTest->SetKeyExpireTime-ExpireTest-1 has failed.");
                return false;
            };
            PrintAction?.Invoke("BMemoryPubSubServiceTest->SetKeyExpireTime-ExpireTest-1 has succeed.");

            bLocalResult = SelectedMemoryService.GetKeyExpireTime(ExpireTestMemoryQueryParameters, out TimeSpan TTL_2,
            (string Message) =>
            {
                bLocalResult = false;
                Console.WriteLine("BMemoryPubSubServiceTest->GetKeyExpireTime-ExpireTest-2->Error-> " + Message);
            });
            if (!bLocalResult)
            {
                PrintAction?.Invoke("BMemoryPubSubServiceTest->GetKeyExpireTime-ExpireTest-2 has failed.");
                return false;
            }
            PrintAction?.Invoke("BMemoryPubSubServiceTest->GetKeyExpireTime-ExpireTest-2 has succeed.");

            bLocalResult = true;
            var ExpireGetKeyResult_1 = SelectedMemoryService.GetKeyValue(ExpireTestMemoryQueryParameters, "TestKey1",
            (string Message) =>
            {
                bLocalResult = false;
                Console.WriteLine("BMemoryPubSubServiceTest->GetKeyValue-ExpireTest-1->Error-> " + Message);
            });
            if (!bLocalResult || ExpireGetKeyResult_1 == null)
            {
                PrintAction?.Invoke("BMemoryPubSubServiceTest->GetKeyValue-ExpireTest-1 has failed.");
                return false;
            }
            if (ExpireGetKeyResult_1.AsString != "TestValue1")
            {
                PrintAction?.Invoke("BMemoryPubSubServiceTest->GetKeyValue-ExpireTest-1 returned unexpected value: " + ExpireGetKeyResult_1?.AsString);
                return false;
            }
            PrintAction?.Invoke("BMemoryPubSubServiceTest->GetKeyValue-ExpireTest-1 has succeed.");

            Thread.Sleep(2500);

            bLocalResult = SelectedMemoryService.GetKeyExpireTime(ExpireTestMemoryQueryParameters, out TimeSpan TTL_3,
            (string Message) =>
            {
                bLocalResult = false;
                Console.WriteLine("BMemoryPubSubServiceTest->GetKeyExpireTime-ExpireTest-3->Error-> Expected: " + Message);
            });
            if (bLocalResult)
            {
                PrintAction?.Invoke("BMemoryPubSubServiceTest->GetKeyExpireTime-ExpireTest-3 did not fail.");
                return false;
            }
            PrintAction?.Invoke("BMemoryPubSubServiceTest->GetKeyExpireTime-ExpireTest-3 successfully failed.");

            var ExpireGetKeyResult_2 = SelectedMemoryService.GetKeyValue(ExpireTestMemoryQueryParameters, "TestKey1",
            (string Message) =>
            {
                Console.WriteLine("BMemoryPubSubServiceTest->GetKeyValue-ExpireTest-2->Error-> " + Message);
            });
            if (ExpireGetKeyResult_2 != null)
            {
                PrintAction?.Invoke("BMemoryPubSubServiceTest->GetKeyValue-ExpireTest-2 did not fail.");
                return false;
            }
            PrintAction?.Invoke("BMemoryPubSubServiceTest->GetKeyValue-ExpireTest-2 successfully expired.");

            bLocalResult = true;
            SelectedMemoryService.GetPubSubService().DeleteTopicGlobally(
                ExpireTestMemoryQueryParameters,
                (string Message) =>
                {
                    bLocalResult = false;
                    Console.WriteLine("BMemoryPubSubServiceTest->DeleteTopicGlobally-2->Error-> " + Message);
                });
            if (!bLocalResult)
            {
                PrintAction?.Invoke("BMemoryPubSubServiceTest->DeleteTopicGlobally-2 has failed.");
                return false;
            }
            PrintAction?.Invoke("BMemoryPubSubServiceTest->DeleteTopicGlobally-2 has succeed.");

            return true;
        }
    }
}