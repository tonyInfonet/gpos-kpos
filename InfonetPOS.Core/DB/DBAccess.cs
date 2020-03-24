using InfonetPOS.Core.DB.Entities;
using InfonetPOS.Core.DB.Interface;
using InfonetPOS.Core.Interfaces;
using MvvmCross;
using MvvmCross.Logging;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InfonetPOS.Core.DB
{
    public class DBAccess : IDBAccess
    {
        private readonly IMvxLog log = Mvx.IoCProvider.Resolve<IMvxLog>();
        private readonly IAppSettings appSettings = Mvx.IoCProvider.Resolve<IAppSettings>();

        private void ExecuteQuery(string connectionString,
                                  string queryString,
                                  Action<SqlDataReader> action,
                                  List<SqlParameter> parameters = null,
                                  bool isStoredProcedure = false)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                using (SqlCommand sqlCommand = new SqlCommand(queryString, connection))
                {
                    if (isStoredProcedure)
                        sqlCommand.CommandType = CommandType.StoredProcedure;

                    if (parameters != null)
                    {
                        foreach (var param in parameters)
                        {
                            sqlCommand.Parameters.Add(param);
                        }
                    }
                    try
                    {
                        connection.Open();
                        using (SqlDataReader dataReader = sqlCommand.ExecuteReader())
                        {
                            action(dataReader);
                            dataReader.Close();
                        }
                    }
                    catch (Exception ex)
                    {
                        log.DebugException(string.Format("DBAccess: {0}", ex.Message), ex);
                        throw ex;
                    }
                    finally
                    {
                        connection.Close();
                    }
                }
            }
        }
        public string GetPassword(string U_CODE)
        {
            try
            {
                log.Debug("DBAccess: Get user password.");
                string epw = null;
                
                var connectionString = appSettings.CSCAdminDBConnectionString;
                const string queryString = @" select EPW from [USER] where U_CODE=@U_CODE";

                ExecuteQuery(connectionString,
                   queryString,
                   (dataReader) =>
                   {
                       if(dataReader.Read())
                       {
                           epw = dataReader[0].ToString();
                       }
                   },
                   new List<SqlParameter>() { new SqlParameter("@U_CODE", System.Data.SqlDbType.NVarChar) {Value= U_CODE } },
                   false);

                if (epw == null)
                    return "";

                var pswd = new EncryptionManager();
                var decryptedText = pswd.DecryptText(epw);
                return decryptedText;
            }
            catch (Exception ex)
            {
                log.DebugException(string.Format("Error: DBAccess:GetPassword() {0}", ex.Message), ex);
                return "";
            }
        }

        public List<string> GetPosIpAddresses()
        {
            try
            {
                log.Debug("DBAccess: Get Pos Ip Addresses.");
                List<string> ips = new List<string>();
                var connectionString = appSettings.CSCAdminDBConnectionString;
                const string queryString = @"SELECT [IP_Address]
                                        FROM [CSCAdmin].[dbo].[POS_IP_Address]";

                ExecuteQuery(connectionString,
                    queryString,
                    dataReader =>
                    {
                        while (dataReader.Read())
                        {
                            ips.Add(dataReader[0].ToString());
                        }
                    });

                return ips;
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        public List<int> GetAllShifts()
        {
            try
            {
                log.Debug("DBAccess: Get All shifts.");
                var connectionString = appSettings.CSCMasterDBConnectionString;
                const string queryString = "    select ShiftNumber from [CSCMaster].[dbo].[ShiftStore] where Active=@Active";
                List<int> shifts = new List<int>();

                ExecuteQuery(connectionString,
                    queryString,
                    (dataReader) =>
                    {
                        while (dataReader.Read())
                        {
                            shifts.Add((int)dataReader[0]);
                        }
                    },
                    new List<SqlParameter>() {
                   new SqlParameter("@Active", System.Data.SqlDbType.Bit){ Value = 1 }
                    });

                return shifts;
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        public List<string> GetAllUsers()
        {
            try
            {
                log.Debug("DBAccess: Get all users.");
                var connectionString = appSettings.CSCAdminDBConnectionString;
                const string queryString = "    select U_CODE from [CSCAdmin].[dbo].[USER]";
                List<string> users = new List<string>();

                ExecuteQuery(connectionString,
                   queryString,
                   (dataReader) =>
                   {
                       while (dataReader.Read())
                       {
                           users.Add((string)dataReader[0]);
                       }
                   });

                return users;
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        public List<Till> GetActiveTills(string userName)
        {
            try
            {
                log.Debug("DBAccess: Get active tills for username {0}.", userName);
                var connectionString = appSettings.CSCMasterDBConnectionString;
                const string queryString = @"Select Till_Num,
                                        ACTIVE,
                                        PROCESS,
                                        ShiftNumber,
                                        POSID,
                                        ShiftDate,
                                        UserLoggedOn
                                        From Tills Where
                                        Tills.Active=@Active
                                        AND Tills.POSID=@POSID
                                        AND Tills.UserLoggedOn=@UserLoggedOn";

                List<Till> tills = new List<Till>();

                ExecuteQuery(connectionString,
                    queryString,
                    (dataReader) =>
                    {
                        while (dataReader.Read())
                        {
                            tills.Add(new Till()
                            {
                                TillNo = (int)dataReader[0],
                                Active = (bool)dataReader[1],
                                Process = (bool)dataReader[2],
                                ShiftNo = Convert.ToInt32((byte)dataReader[3]),
                                PosID = Convert.ToInt32((byte)dataReader[4]),
                                ShiftDate = ConvertToNullableDateTime(dataReader[5].ToString()),
                                UserLoggedOn = dataReader[6].ToString()
                            });
                        }
                    },
                    new List<SqlParameter>() {
                   new SqlParameter("@Active", System.Data.SqlDbType.Bit){ Value = 1 },
                   new SqlParameter("@POSID", System.Data.SqlDbType.TinyInt){Value = int.Parse(appSettings.PosId)},
                   new SqlParameter("@UserLoggedOn", System.Data.SqlDbType.NVarChar){Value = userName}
                    });

                return tills;
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        public List<Till> GetAvailableTills()
        {
            try
            {
                log.Debug("DBAccess: Get available tills.");
                var connectionString = appSettings.CSCMasterDBConnectionString;
                const string queryString = @"Select Till_Num,
                                        ACTIVE,
                                        PROCESS,
                                        ShiftNumber,
                                        POSID,
                                        ShiftDate,
                                        UserLoggedOn
                                        From Tills Where 
                                        Tills.Active <> @Active
                                        AND Tills.Till_Num <> @Till_Num";
                List<Till> tills = new List<Till>();

                ExecuteQuery(connectionString,
                    queryString,
                    (dataReader) =>
                    {
                        while (dataReader.Read())
                        {
                            tills.Add(new Till()
                            {
                                TillNo = (int)dataReader[0],
                                Active = (bool)dataReader[1],
                                Process = (bool)dataReader[2],
                                ShiftNo = Convert.ToInt32((byte)dataReader[3]),
                                PosID = Convert.ToInt32((byte)dataReader[4]),
                                ShiftDate = ConvertToNullableDateTime(dataReader[5].ToString()),
                                UserLoggedOn = dataReader[6].ToString()
                            });
                        }
                    },
                    new List<SqlParameter>() {
                   new SqlParameter("@Active", System.Data.SqlDbType.Bit){ Value = 1 },
                   new SqlParameter("@Till_Num", System.Data.SqlDbType.Int){Value = 100},
                    });

                return tills;
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        public bool StopTill(int tillNo)
        {
            log.Debug("DBAccess: Stop till {0}", tillNo);
            return StopTillProcessing(tillNo, 1);
        }

        public bool CloseTill(int tillNo)
        {
            log.Debug("DBAccess: Close till {0}", tillNo);
            return StopTillProcessing(tillNo, 0);
        }

        public bool CSCTillsMoveRecords(DateTime? shiftDate, int tillNo)
        {
            try
            {
                log.Debug("DBAccess: CSC Tills Move Records.");
                var connectionString = appSettings.CSCTillsDBConnectionString;
                const string queryString = @"MoveRecords";
                ExecuteQuery(connectionString,
                   queryString,
                   (dataReader) => { },
                   new List<SqlParameter>() {
                   new SqlParameter("@CurShiftDate", System.Data.SqlDbType.DateTime){Value = shiftDate},
                   new SqlParameter("@TillID", System.Data.SqlDbType.Int){Value = tillNo}
                   },
                   true);

                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        private bool StopTillProcessing(int tillNo, int keepActive)
        {
            try
            {
                log.Debug("DBAccess: Stop Till {0} processing where Active:{1}", tillNo, keepActive);
                var connectionString = appSettings.CSCMasterDBConnectionString;
                const string queryString = @"  
                     update [CSCMaster].[dbo].[TILLS]
                     set 
                     ACTIVE=@Active,
                     PROCESS=@Process
                     where TILL_NUM=@TillNo";
                ExecuteQuery(connectionString,
                   queryString,
                   (dataReader) => { },
                   new List<SqlParameter>() {
                   new SqlParameter("@Active", System.Data.SqlDbType.Bit){ Value = keepActive },
                   new SqlParameter("@Process", System.Data.SqlDbType.Bit){Value = 0},
                   new SqlParameter("@TillNo", System.Data.SqlDbType.Int){Value = tillNo}
                   });

                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public bool StartTill(int tillNo)
        {
            try
            {
                log.Debug("DBAccess: Start Till {0}.", tillNo);
                var connectionString = appSettings.CSCMasterDBConnectionString;
                const string queryString = @"  
                     update [CSCMaster].[dbo].[TILLS]
                     set 
                     PROCESS=@Process
                     where TILL_NUM=@TillNo";
                ExecuteQuery(connectionString,
                   queryString,
                   (dataReader) => { },
                   new List<SqlParameter>() {
                   new SqlParameter("@Process", System.Data.SqlDbType.Bit){Value = 1},
                   new SqlParameter("@TillNo", System.Data.SqlDbType.Int){Value = tillNo}
                   });

                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public bool OpenTill(string userName, int shiftNo, int tillNo, DateTime? shiftDate)
        {
            try
            {
                log.Debug("DBAccess: Open till {0} , username:{1},shift:{2}", tillNo, userName, shiftNo);
                var connectionString = appSettings.CSCMasterDBConnectionString;
                const string queryString = @"  
                     update [CSCMaster].[dbo].[TILLS]
                     set ACTIVE=@Active,
                     PROCESS=@Process,
                     DATE_OPEN=@DateOpen,
                     TIME_OPEN=@TimeOpen,
                     ShiftDate=@ShiftDate,
                     ShiftNumber=@ShiftNumber,
                     POSID=@PosID,
                     UserLoggedOn=@UserLoggedOn
                     where TILL_NUM=@TillNo";
                ExecuteQuery(connectionString,
                   queryString,
                   (dataReader) => { },
                   new List<SqlParameter>() {
                   new SqlParameter("@Active", System.Data.SqlDbType.Bit){ Value = 1 },
                   new SqlParameter("@Process", System.Data.SqlDbType.Bit){Value = 1},
                   new SqlParameter("@DateOpen", System.Data.SqlDbType.DateTime){Value = DateTime.Now},
                   new SqlParameter("@TimeOpen", System.Data.SqlDbType.DateTime){Value = DateTime.Now},
                   new SqlParameter("@ShiftDate", System.Data.SqlDbType.DateTime){Value = shiftDate},
                   new SqlParameter("@ShiftNumber", System.Data.SqlDbType.Int){Value = shiftNo},
                   new SqlParameter("@PosID", System.Data.SqlDbType.TinyInt){Value = int.Parse(appSettings.PosId)},
                   new SqlParameter("@UserLoggedOn", System.Data.SqlDbType.NVarChar){Value = userName},
                   new SqlParameter("@TillNo", System.Data.SqlDbType.Int){Value = tillNo}
                   });

                return true;
            }
            catch (Exception ex)
            {
                return false;
            }

        }

        public List<Tender> GetTenders()
        {
            try
            {
                log.Debug("DBAccess: Get Tenders.");
                var appSettings = Mvx.IoCProvider.Resolve<IAppSettings>();
                var connectionString = appSettings.CSCMasterDBConnectionString;
                List<Tender> tenders = new List<Tender>();

                var queryString = @"
                SELECT TENDCLASS, TENDDESC, EXCHANGE, GIVEASREF
                FROM[CSCMaster].[dbo].TENDMAST 
                where Inactive = 0 
                AND(TENDCLASS = 'CASH' OR TENDCLASS = 'CRCARD' OR TENDCLASS = 'DBCARD')
				ORDER BY DISPLAYSEQ ";

                ExecuteQuery(connectionString,
                    queryString,
                    (dataReader) =>
                    {
                        while (dataReader.Read())
                        {
                            tenders.Add(new Tender((string)dataReader[0])
                            {
                                Description = (string)dataReader[1],
                                ExchangeRate = Convert.ToDouble((decimal)dataReader[2]),
                                GiveAsRef = (bool)dataReader[3]
                            });
                        }
                    });

                return tenders;
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        public Company GetCompany()
        {
            try
            {
                log.Debug("DBAccess: Get Company.");
                Company company = null;
                var connectionString = appSettings.CSCAdminDBConnectionString;
                const string queryString = @"SELECT TOP (1) [Company_Name]
                                        ,[Address_1]
                                        ,[Address_2]
                                        ,[City]
                                        ,[Province]
                                        FROM [CSCAdmin].[dbo].[Setup_C]";

                ExecuteQuery(connectionString,
                    queryString,
                    (dataReader) =>
                    {
                        while (dataReader.Read())
                        {
                            company = new Company()
                            {
                                CompanyName = dataReader[0].ToString(),
                                Address1 = dataReader[1].ToString(),
                                Address2 = dataReader[2].ToString(),
                                City = dataReader[3].ToString(),
                                Province = dataReader[4].ToString(),
                            };
                            break;
                        }
                    });

                return company;
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        public string GetInvoiceNo()
        {
            try
            {
                log.Debug("DBAccess: Get Invoice no.");
                int invoiceNo = 0;
                var returnValue = new List<SqlParameter>()
                {
                     new SqlParameter(){Direction = ParameterDirection.ReturnValue}
                };
                var connectionString = appSettings.CSCAdminDBConnectionString;
                const string queryString = @"GetSaleNumber";

                ExecuteQuery(connectionString,
                   queryString,
                   (dataReader) =>
                   {
                   },
                   returnValue,
                   true);

                invoiceNo = (int)returnValue[0].Value;
                return invoiceNo.ToString();
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        private DateTime? ConvertToNullableDateTime(string dateString)
        {
            try
            {
                log.Debug("DBAccess: Convert DateTime to Nullable DateTime.");
                DateTime? convertedDate;
                bool success = DateTime.TryParse(dateString, out DateTime date);
                if (success)
                {
                    convertedDate = date;
                }
                else
                {
                    convertedDate = null;
                }
                return convertedDate;
            }
            catch (Exception ex)
            {
                log.DebugException(string.Format("DBAccess: {0}", ex.Message), ex);
                return null;
            }
        }

    }
}
