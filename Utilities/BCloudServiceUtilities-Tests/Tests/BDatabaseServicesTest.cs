/// MIT License, Copyright Burak Kara, burak@burak.io, https://en.wikipedia.org/wiki/MIT_License

using System;
using System.Collections.Generic;
using BCloudServiceUtilities;
using BCommonUtilities;
using Newtonsoft.Json.Linq;

namespace BCloudServiceUtilitiesTest.Tests
{
    public class BDatabaseServicesTest
    {
        private readonly IBDatabaseServiceInterface SelectedDBService;

        private readonly string TableName;
        private readonly string Key;

        private readonly Action<string> PrintAction;

        public BDatabaseServicesTest(IBDatabaseServiceInterface _DBService, string _TableName, string _StringKey, Action<string> _PrintAction)
        {
            SelectedDBService = _DBService;
            TableName = _TableName;
            Key = _StringKey;
            PrintAction = _PrintAction;
        }

        public bool Start()
        {
            PrintAction?.Invoke("BDatabaseServicesTest->Info-> Test is starting.");

            if (SelectedDBService == null)
            {
                PrintAction?.Invoke("BDatabaseServicesTest->Error-> Given SelectedDBService is null.");
                return false;
            }

            if (TableName == null || TableName.Length == 0)
            {
                PrintAction?.Invoke("BDatabaseServicesTest->Error-> Given TableName is null or empty.");
                return false;
            }

            if (Key == null || Key.Length == 0)
            {
                PrintAction?.Invoke("BDatabaseServicesTest->Error-> Given Key is null or empty.");
                return false;
            }

            if (!SelectedDBService.HasInitializationSucceed())
            {
                PrintAction?.Invoke("BDatabaseServicesTest->Error-> Initialization failed.");
                return false;
            }
            PrintAction?.Invoke("BDatabaseServicesTest->Log-> Initialization succeed.");

            PreCleanup();

            if (!TestPutItem(1)) return false;

            PrintAction?.Invoke("BDatabaseServicesTest->Log-> Conditional put item, expecting failure.");

            if (TestPutItem(2, SelectedDBService.BuildAttributeNotExistCondition(Key))) return false;

            if (!TestGetItem(3)) return false;

            if (!TestUpdateItem(4)) return false;

            PrintAction?.Invoke("BDatabaseServicesTest->Log-> Conditional update item, expecting failure.");

            if (TestUpdateItem(5,
                SelectedDBService.BuildAttributeEqualsCondition("Test_Attr_Key_2", new BPrimitiveType(345)))) return false;

            if (!TestUpdateItem(6,
                SelectedDBService.BuildAttributeNotEqualsCondition("Test_Attr_Key_2", new BPrimitiveType(345)))) return false;

            if (!TestUpdateItem(7,
                SelectedDBService.BuildAttributeGreaterOrEqualCondition("Test_Attr_Key_2", new BPrimitiveType(22)))) return false;

            if (!TestUpdateItem(8,
                SelectedDBService.BuildAttributeGreaterCondition("Test_Attr_Key_2", new BPrimitiveType(21)))) return false;

            if (!TestUpdateItem(9,
                SelectedDBService.BuildAttributeLessCondition("Test_Attr_Key_2", new BPrimitiveType(23)))) return false;

            if (!TestUpdateItem(10,
                SelectedDBService.BuildAttributeLessOrEqualCondition("Test_Attr_Key_2", new BPrimitiveType(22)))) return false;

            if (TestUpdateItem(11,
                SelectedDBService.BuildAttributeEqualsCondition("Test_Attr_Key_1", new BPrimitiveType("Test_Attr_Val_New_2")))) return false;

            if (TestUpdateItem(12,
                SelectedDBService.BuildAttributeNotEqualsCondition("Test_Attr_Key_1", new BPrimitiveType("Test_Attr_Val_New_1")))) return false;

            if (!TestUpdateItem(13,
                SelectedDBService.BuildAttributeEqualsCondition("Test_Attr_Key_1", new BPrimitiveType("Test_Attr_Val_New_1")))) return false;

            if (!TestUpdateItem(14, null, true)) return false;

            if (!TestGetItem(15)) return false;

            if (!TestIncrement(16)) return false;

            if (!TestScanTable(17)) return false;

            if (!TestDeleteItem(18)) return false;

            if (!TestAddToList(19)) return false;

            if (!TestRemoveFromList(20)) return false;

            return true;
        }

        private void PreCleanup()
        {
            SelectedDBService.DeleteItem(
                TableName,
                Key, new BPrimitiveType("test_1"),
                out JObject Ignore,
                EBReturnItemBehaviour.DoNotReturn,
                null);
        }

        private bool TestAddToList(int _TestStepNo)
        {
            PrintAction?.Invoke("Step " + _TestStepNo + "->" + "TestAddToList->Log-> Testing AddElementsToArrayItem-1...");

            bool bLocalFailure = !SelectedDBService.AddElementsToArrayItem(
                TableName,
                Key, new BPrimitiveType("test_1"),
                "test_list", new BPrimitiveType[]
                {
                    new BPrimitiveType("test_value_1"),
                    new BPrimitiveType("test_value_2"),
                    new BPrimitiveType("test_value_3"),
                    new BPrimitiveType("test_value_4")
                },
                out JObject ReturnValue,
                EBReturnItemBehaviour.ReturnAllOld,
                null,
                (string Message) =>
                {
                    Console.WriteLine("Step " + _TestStepNo + "->" + "TestAddToList->Error-> " + Message);
                    bLocalFailure = true;
                });

            if (bLocalFailure)
            {
                PrintAction?.Invoke("Step " + _TestStepNo + "->" + "TestAddToList->Error-> AddElementsToArrayItem-1 failed.");
                return false;
            }
            PrintAction?.Invoke("Step " + _TestStepNo + "->" + "TestAddToList->Log-> AddElementsToArrayItem-1 succeed. Returned: " + ReturnValue?.ToString());

            PrintAction?.Invoke("Step " + _TestStepNo + "->" + "TestAddToList->Log-> Testing AddElementsToArrayItem-2. Expecting failure...");

            bLocalFailure = !SelectedDBService.AddElementsToArrayItem(
                TableName,
                Key, new BPrimitiveType("test_1"),
                "test_list", new BPrimitiveType[]
                {
                    new BPrimitiveType("test_value_2"),
                    new BPrimitiveType("test_value_5")
                },
                out ReturnValue,
                EBReturnItemBehaviour.ReturnAllOld,
                SelectedDBService.BuildArrayElementNotExistCondition(new BPrimitiveType("test_value_1")),
                (string Message) =>
                {
                    Console.WriteLine("Step " + _TestStepNo + "->" + "TestAddToList->Error-> " + Message);
                    bLocalFailure = true;
                });
            if (!bLocalFailure)
            {
                PrintAction?.Invoke("Step " + _TestStepNo + "->" + "TestAddToList->Error-> AddElementsToArrayItem-2 did not fail.");
                return false;
            }
            PrintAction?.Invoke("Step " + _TestStepNo + "->" + "TestAddToList->Log-> AddElementsToArrayItem-2 successfully failed. Returned: " + ReturnValue?.ToString());

            bLocalFailure = !SelectedDBService.GetItem(
                TableName,
                Key, new BPrimitiveType("test_1"),
                new string[] { "test_list" },
                out ReturnValue,
                (string Message) =>
                {
                    Console.WriteLine("Step " + _TestStepNo + "->" + "TestAddToList->Error-> " + Message);
                    bLocalFailure = true;
                });
            if (bLocalFailure)
            {
                PrintAction?.Invoke("Step " + _TestStepNo + "->" + "TestAddToList->Error-> GetItem-1 has failed.");
                return false;
            }
            PrintAction?.Invoke("Step " + _TestStepNo + "->" + "TestAddToList->Log-> GetItem-1 successfully completed. Returned: " + ReturnValue?.ToString());

            return true;
        }

        private bool TestRemoveFromList(int _TestStepNo)
        {
            PrintAction?.Invoke("Step " + _TestStepNo + "->" + "TestRemoveFromList->Log-> Testing RemoveElementsFromArrayItem...");

            bool bLocalFailure = !SelectedDBService.RemoveElementsFromArrayItem(
                TableName,
                Key, new BPrimitiveType("test_1"),
                "test_list", new BPrimitiveType[]
                {
                    new BPrimitiveType("test_value_1"),
                    new BPrimitiveType("test_value_6")
                },
                out JObject ReturnValue,
                EBReturnItemBehaviour.ReturnAllNew,
                (string Message) =>
                {
                    Console.WriteLine("Step " + _TestStepNo + "->" + "TestRemoveFromList->Error-> " + Message);
                    bLocalFailure = true;
                });

            if (bLocalFailure)
            {
                PrintAction?.Invoke("Step " + _TestStepNo + "->" + "TestRemoveFromList->Error-> RemoveElementsFromArrayItem failed.");
                return false;
            }
            PrintAction?.Invoke("Step " + _TestStepNo + "->" + "TestRemoveFromList->Log-> RemoveElementsFromArrayItem succeed. Returned: " + ReturnValue?.ToString());

            return true;
        }

        private bool TestPutItem(int _TestStepNo, BDatabaseAttributeCondition _ConditionExpression = null)
        {
            //Test put item
            PrintAction?.Invoke("Step " + _TestStepNo + "->" + "TestPutItem->Log-> Testing PutItem...");
            bool bLocalFailure = !SelectedDBService.PutItem(
                TableName,
                Key, new BPrimitiveType("test_1"),
                new JObject(
                        new JProperty(Key, "test_1"),
                        new JProperty("Test_Attr_Key_1", "Test_Attr_Val_1"),
                        new JProperty("Test_Attr_Key_2", 2),
                        new JProperty("Test_Attr_Key_3", 3.0f),
                        new JProperty("Test_Attr_Key_4",
                            new JArray(
                                    new JValue("Array_Elem_1"),
                                    new JValue(2),
                                    new JValue(3.0f)
                                ))),
                out JObject Result_1,
                EBReturnItemBehaviour.ReturnAllOld,
                _ConditionExpression,
                (string Message) =>
                {
                    Console.WriteLine("Step " + _TestStepNo + "->" + "TestPutItem->Error-> " + Message);
                    bLocalFailure = true;
                });
            if (bLocalFailure)
            {
                PrintAction?.Invoke("Step " + _TestStepNo + "->" + "TestPutItem->Error-> PutItem failed.");
                return false;
            }
            PrintAction?.Invoke("Step " + _TestStepNo + "->" + "TestPutItem->Log-> PutItem succeed. Returned: " + Result_1?.ToString());
            return true;
        }

        private bool TestUpdateItem(int _TestStepNo, BDatabaseAttributeCondition _ConditionExpression = null, bool bUpdateOnlyTwoAttributes = false)
        {
            JObject NewItem = null;
            if (bUpdateOnlyTwoAttributes)
            {
                NewItem = new JObject(
                        new JProperty(Key, "test_1"),
                        new JProperty("Test_Attr_Key_2", 11115.0f),
                        new JProperty("Test_Attr_Key_4",
                            new JArray(
                                    new JValue(123123123.0f)
                                )));
            }
            else
            {
                NewItem = new JObject(
                        new JProperty(Key, "test_1"),
                        new JProperty("Test_Attr_Key_1", "Test_Attr_Val_New_1"),
                        new JProperty("Test_Attr_Key_2", 22),
                        new JProperty("Test_Attr_Key_3", 33.0f),
                        new JProperty("Test_Attr_Key_4",
                            new JArray(
                                    new JValue("Array_Elem_New_1"),
                                    new JValue(22),
                                    new JValue(33.0f)
                                )));
            }

            //Test update item
            PrintAction?.Invoke("Step " + _TestStepNo + "->" + "TestUpdateItem->Log-> Testing UpdateItem...");
            bool bLocalFailure = !SelectedDBService.UpdateItem(
                TableName,
                Key, new BPrimitiveType("test_1"),
                NewItem,
                out JObject Result_1,
                EBReturnItemBehaviour.ReturnAllOld,
                _ConditionExpression,
                (string Message) =>
                {
                    Console.WriteLine("Step " + _TestStepNo + "->" + "TestUpdateItem->Error-> " + Message);
                    bLocalFailure = true;
                });
            if (bLocalFailure)
            {
                PrintAction?.Invoke("Step " + _TestStepNo + "->" + "TestUpdateItem->Error-> UpdateItem failed.");
                return false;
            }
            PrintAction?.Invoke("Step " + _TestStepNo + "->" + "TestUpdateItem->Log-> UpdateItem succeed. Returned: " + Result_1?.ToString());
            return true;
        }

        private bool TestGetItem(int _TestStepNo)
        {
            //Test get item
            PrintAction?.Invoke("Step " + _TestStepNo + "->" + "TestGetItem->Log-> Testing GetItem...");
            bool bLocalFailure = !SelectedDBService.GetItem(
                TableName,
                Key, new BPrimitiveType("test_1"),
                new string[]
                {
                    "Test_Attr_Key_1", "Test_Attr_Key_2", "Test_Attr_Key_3", "Test_Attr_Key_4"
                },
                out JObject Result_2,
                (string Message) =>
                {
                    Console.WriteLine("Step " + _TestStepNo + "->" + "TestGetItem->Error-> " + Message);
                    bLocalFailure = true;
                });
            if (bLocalFailure)
            {
                PrintAction?.Invoke("Step " + _TestStepNo + "->" + "TestGetItem->Error-> GetItem failed.");
                return false;
            }
            PrintAction?.Invoke("Step " + _TestStepNo + "->" + "TestGetItem->Log-> GetItem succeed. Returned: " + Result_2?.ToString());
            return true;
        }

        private bool TestIncrement(int _TestStepNo)
        {
            //Test increment
            PrintAction?.Invoke("Step " + _TestStepNo + "->" + "TestIncrement->Log-> Testing IncrementOrDecrementItemValue...");

            //Previous value ("Test_Attr_Key_2", 11115.0f),
            bool bLocalFailure = !SelectedDBService.IncrementOrDecrementItemValue(
                TableName,
                Key, new BPrimitiveType("test_1"),
                out double NewValue,
                "Test_Attr_Key_2",
                11120.0f,
                true,
                (string Message) =>
                {
                    Console.WriteLine("Step " + _TestStepNo + "->" + "TestIncrement->Error-> " + Message);
                    bLocalFailure = true;
                });
            if (bLocalFailure)
            {
                PrintAction?.Invoke("Step " + _TestStepNo + "->" + "TestIncrement->Error-> IncrementOrDecrementItemValue failed.");
                return false;
            }

            if (NewValue == -5.0f)
            {
                PrintAction?.Invoke("Step " + _TestStepNo + "->" + "TestIncrement->Log-> IncrementOrDecrementItemValue succeed. Returned: " + NewValue);
                return true;
            }
            PrintAction?.Invoke("Step " + _TestStepNo + "->" + "TestIncrement->Log-> IncrementOrDecrementItemValue failed. Returned: " + NewValue);
            return false;
        }

        private bool TestScanTable(int _TestStepNo)
        {
            //Test scan items
            PrintAction?.Invoke("Step " + _TestStepNo + "->" + "TestScanTable->Log-> Testing ScanTable...");
            bool bLocalFailure = !SelectedDBService.ScanTable(
                TableName,
                out List<JObject> Result_4,
                (string Message) =>
                {
                    Console.WriteLine("Step " + _TestStepNo + "->" + "TestScanTable->Error-> " + Message);
                    bLocalFailure = true;
                });
            if (bLocalFailure)
            {
                PrintAction?.Invoke("Step " + _TestStepNo + "->" + "TestScanTable->Error-> ScanTable failed.");
                return false;
            }

            string Readable = "";
            if (Result_4 != null)
            {
                foreach (var Cur in Result_4)
                {
                    Readable += Cur.ToString() + "\n";
                }
            }
            
            PrintAction?.Invoke("Step " + _TestStepNo + "->" + "TestScanTable->Log-> ScanTable succeed. Returned: " + Readable);
            return true;
        }

        private bool TestDeleteItem(int _TestStepNo)
        {
            //Test delete item
            PrintAction?.Invoke("Step " + _TestStepNo + "->" + "TestDeleteItem->Log-> Testing DeleteItem...");
            bool bLocalFailure = !SelectedDBService.DeleteItem(
                TableName,
                Key, new BPrimitiveType("test_1"),
                out JObject Result_3,
                EBReturnItemBehaviour.ReturnAllOld,
                (string Message) =>
                {
                    Console.WriteLine("Step " + _TestStepNo + "->" + "TestDeleteItem->Error-> " + Message);
                    bLocalFailure = true;
                }
                );
            if (bLocalFailure)
            {
                PrintAction?.Invoke("Step " + _TestStepNo + "->" + "TestDeleteItem->Error-> DeleteItem failed.");
                return false;
            }
            PrintAction?.Invoke("Step " + _TestStepNo + "->" + "TestDeleteItem->Log-> DeleteItem succeed. Returned: " + Result_3?.ToString());
            return true;
        }
    }
}