using System;
using System.Buffers.Text;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using System.Reflection;
using Newtonsoft.Json;
using System.IO;



namespace Banking_Application
{
    public sealed class Data_Access_Layer
    {

        private List<Bank_Account> accounts;
        private static String databaseName = "Banking Database.db";

        // provide a thread-safe 
        private static readonly Lazy<Data_Access_Layer> lazyInstance = new Lazy<Data_Access_Layer>(() => new Data_Access_Layer());

        private Data_Access_Layer()
        {
            // Initialize the AesEncrypti
            accounts = new List<Bank_Account>();
        }

        //Using lock keyword or by using Lazy<T> for lazy initialization.
        public static Data_Access_Layer getInstance()
        {
            return lazyInstance.Value;
        }

        private SqliteConnection getDatabaseConnection()
        {

            String databaseConnectionString = new SqliteConnectionStringBuilder()
            {
                DataSource = Data_Access_Layer.databaseName,
                Mode = SqliteOpenMode.ReadWriteCreate
            }.ToString();

            return new SqliteConnection(databaseConnectionString);
        }

        private void initialiseDatabase()
        {
            using (var connection = getDatabaseConnection())
            {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText =
                @"
                    CREATE TABLE IF NOT EXISTS Bank_Accounts(    
                        accountNo TEXT PRIMARY KEY,
                        name TEXT NOT NULL,
                        address_line_1 TEXT,
                        address_line_2 TEXT,
                        address_line_3 TEXT,
                        town TEXT NOT NULL,
                        balance REAL NOT NULL,
                        accountType INTEGER NOT NULL,
                        overdraftAmount REAL,
                        interestRate REAL, 
                        iv TEXT NOT NULL, 
                        integrity_hash TEXT NOT NULL 

                    ) WITHOUT ROWID
                ";

                command.ExecuteNonQuery();

            }
        }

        public Bank_Account FindBankAccountByAccNo(String accNo)
        {
            // Log the event: Attempting to find bank account by account number
            EventLogger.WriteEvent($"Attempting to find bank account by account number: {accNo}", EventLogEntryType.Information, DateTime.Now);

            String encryptedAccNo = AesEncryptionHandler.EncryptAccountNumber(accNo);

            Bank_Account ba = LoadBankAccountFromDatabase(encryptedAccNo);

            if (ba == null)
                return null;
            else 
                return ba;
        }

        private Bank_Account LoadBankAccountFromDatabase(String encryptedAccNo)
        {
            // Ensure the database is initialized
            if (!File.Exists(Data_Access_Layer.databaseName))
                initialiseDatabase();

            // Check if the account is already loaded in the accounts list
            Bank_Account existingAccount = accounts.FirstOrDefault(acc => acc.AccountNo.Equals(encryptedAccNo, StringComparison.OrdinalIgnoreCase));

            if (existingAccount != null)
            {
                return existingAccount;
            }



            // If the account is not already loaded, load it from the database
            using (var connection = getDatabaseConnection())
            {
                connection.Open();
                var command = connection.CreateCommand();
                // Remove Selected All * 
                command.CommandText = "SELECT accountNo,name, address_line_1, address_line_2, address_line_3, town, balance, accountType, overdraftAmount, interestRate, iv,integrity_hash   FROM Bank_Accounts WHERE accountNo = @accountNo";
                command.Parameters.AddWithValue("@accountNo", encryptedAccNo);


                SqliteDataReader dr = command.ExecuteReader();

                while (dr.Read())
                {
                    int accountType = dr.GetInt16(7);

                    if (accountType == Account_Type.Current_Account)
                    {
                        // Get from database the encrypted data 
                        string encryptedNameBytes =dr.GetString(1);
                        string encryptedAddressLine1Bytes = dr.GetString(2);
                        string encryptedAddressLine2Bytes = dr.GetString(3);
                        string encryptedAddressLine3Bytes = dr.GetString(4);
                        string encryptedTownBytes = dr.GetString(5);

                        string ivBase64 = dr.GetString(10); // Read IV as Base64 string from the database
                        byte[] iv = Convert.FromBase64String(ivBase64); // Convert IV to byte array for decryption

                        Current_Account ca = new Current_Account();

                        ca.AccountNo = AesEncryptionHandler.DecryptAccountNumber(dr.GetString(0));
                        ca.Name = AesEncryptionHandler.Decrypt(encryptedNameBytes, AesEncryptionHandler.GetOrCreateAesEncryptionKeyCBC(iv));
                        ca.Address_line_1 = AesEncryptionHandler.Decrypt(encryptedAddressLine1Bytes, AesEncryptionHandler.GetOrCreateAesEncryptionKeyCBC(iv));
                        ca.Address_line_2 = AesEncryptionHandler.Decrypt(encryptedAddressLine2Bytes, AesEncryptionHandler.GetOrCreateAesEncryptionKeyCBC(iv));
                        ca.Address_line_3 = AesEncryptionHandler.Decrypt(encryptedAddressLine3Bytes, AesEncryptionHandler.GetOrCreateAesEncryptionKeyCBC(iv));
                        ca.Town = AesEncryptionHandler.Decrypt(encryptedTownBytes, AesEncryptionHandler.GetOrCreateAesEncryptionKeyCBC(iv));
                        ca.Balance = dr.GetDouble(6);
                        ca.overdraftAmount = dr.GetDouble(8);

                        try
                        {
                            string recalculate_integrity_hash = calculateIntegrityHash(ca);

                            if (recalculate_integrity_hash != dr.GetString(11))
                            {
                                Console.WriteLine("Integrity check failed: Data has been altered or corrupted. Security risk detected.");

                                // Log the event or take other appropriate actions
                                EventLogger.WriteEvent($"Integrity check failed for account: {AesEncryptionHandler.DecryptAccountNumber(ca.AccountNo)}. Security risk detected.", EventLogEntryType.Error, DateTime.Now);
                                throw new FormatException("Integrity check failed: Access to this account is temporarily blocked for security reasons.");
                            }
                            else
                            {
                                return ca;
                            }
                        }
                        catch (System.FormatException ex)
                        {
                            // Handle the exception gracefully without breaking the code
                            Console.WriteLine($"Exception: {ex.Message}");
                        }
                    }
                    else
                    {

                        // Get from database the encrypted data 
                        string encryptedNameBytes = dr.GetString(1);
                        string encryptedAddressLine1Bytes = dr.GetString(2);
                        string encryptedAddressLine2Bytes = dr.GetString(3);
                        string encryptedAddressLine3Bytes = dr.GetString(4);
                        string encryptedTownBytes = dr.GetString(5);

                        string ivBase64 = dr.GetString(10); // Read IV as Base64 string from the database

                        byte[] iv = Convert.FromBase64String(ivBase64); // Convert IV to byte array for decryption


                        Savings_Account sa = new Savings_Account();

                        sa.AccountNo = AesEncryptionHandler.DecryptAccountNumber(dr.GetString(0));
                        sa.Name =AesEncryptionHandler.Decrypt(encryptedNameBytes, AesEncryptionHandler.GetOrCreateAesEncryptionKeyCBC(iv));
                        sa.Address_line_1 = AesEncryptionHandler.Decrypt(encryptedAddressLine1Bytes, AesEncryptionHandler.GetOrCreateAesEncryptionKeyCBC(iv));
                        sa.Address_line_2 = AesEncryptionHandler.Decrypt(encryptedAddressLine2Bytes, AesEncryptionHandler.GetOrCreateAesEncryptionKeyCBC(iv));
                        sa.Address_line_3 = AesEncryptionHandler.Decrypt(encryptedAddressLine3Bytes, AesEncryptionHandler.GetOrCreateAesEncryptionKeyCBC(iv));
                        sa.Town = AesEncryptionHandler.Decrypt(encryptedTownBytes, AesEncryptionHandler.GetOrCreateAesEncryptionKeyCBC(iv));
                        sa.Balance = dr.GetDouble(6);
                        sa.interestRate = dr.GetDouble(9);

                    
                        try
                        {
                            // Integrity check for Savings_Account
                            string recalculate_integrity_hash = calculateIntegrityHash(sa);

                            if (recalculate_integrity_hash != dr.GetString(11))
                            {
                                Console.WriteLine("Integrity check failed: Data has been altered or corrupted. Security risk detected.");

                                // Log the event or take other appropriate actions
                                EventLogger.WriteEvent($"Integrity check failed for account: {AesEncryptionHandler.DecryptAccountNumber(sa.AccountNo)}. Security risk detected.", EventLogEntryType.Error, DateTime.Now);

                                throw new FormatException("Integrity check failed: Access to this account is temporarily blocked for security reasons.");
                            }
                            else
                            {
                                return sa;
                            }
                        }
                        catch (System.FormatException ex)
                        {
                            // Handle the exception gracefully without breaking the code
                            Console.WriteLine($"Exception: {ex.Message}");
                           
                        }
                    }
                }
            }

            // Account not found in the database

            return null;
        }

        public String AddBankAccount(Bank_Account ba)
        {

            // Get the call stack
            StackTrace stackTrace = new StackTrace();

            // Check if the caller is the main method or part of the system
            if (!IsSystemAndMainCaller(stackTrace))
            {
                // Log Tracer 
                LogStackTrace();
                // Log the event: Unauthorized caller
                EventLogger.WriteEvent($"AddBankAccount failed. Unauthorized caller.", EventLogEntryType.Error, DateTime.Now);
                return null;
            }

            // Check available free space on the drive before proceeding
            DriveInfo driveInfo = new DriveInfo(Path.GetPathRoot(AppDomain.CurrentDomain.BaseDirectory));
            long availableFreeSpace = driveInfo.AvailableFreeSpace;

            // The minimum required free space
            long requiredFreeSpace = 1024 * 1024 * 100;

            if (availableFreeSpace < requiredFreeSpace)
            {
                // Log the event: Insufficient free space
                EventLogger.WriteEvent($"AddBankAccount failed. Insufficient free space on the drive.", EventLogEntryType.Error, DateTime.Now);
                LogStackTrace(); // Log the stack trace
                return null;
            }

            // Ensure the database is initialized
            if (!File.Exists(Data_Access_Layer.databaseName))
                initialiseDatabase();


            if (ba.GetType() == typeof(Current_Account))
                ba = (Current_Account)ba;
            else
                ba = (Savings_Account)ba;

            accounts.Add(ba);

            using (var connection = getDatabaseConnection())
            {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText =
                @"
                    INSERT INTO Bank_Accounts VALUES(
                    @accountNo, 
                    @name, 
                    @address_line_1, 
                    @address_line_2, 
                    @address_line_3, 
                    @town, 
                    @balance, 
                    @accountType, 
                    @overdraftAmount, 
                    @interestRate, 
                    @iv, 
                    @integrity_hash)";

             

                byte[] iv = generateRandomIV();
                Aes aes = AesEncryptionHandler.GetOrCreateAesEncryptionKeyCBC(iv);

                // Add parameters directly to the command's Parameters collection
                command.Parameters.AddWithValue("@accountNo", (AesEncryptionHandler.EncryptAccountNumber(ba.AccountNo)));
                command.Parameters.AddWithValue("@name", Convert.ToBase64String(AesEncryptionHandler.Encrypt(Encoding.UTF8.GetBytes(ba.Name), aes)));
                command.Parameters.AddWithValue("@address_line_1", Convert.ToBase64String(AesEncryptionHandler.Encrypt(Encoding.UTF8.GetBytes(ba.Address_line_1), aes)));
                command.Parameters.AddWithValue("@address_line_2", Convert.ToBase64String(AesEncryptionHandler.Encrypt(Encoding.UTF8.GetBytes(ba.Address_line_2), aes)));
                command.Parameters.AddWithValue("@address_line_3", Convert.ToBase64String(AesEncryptionHandler.Encrypt(Encoding.UTF8.GetBytes(ba.Address_line_3), aes)));
                command.Parameters.AddWithValue("@town", Convert.ToBase64String(AesEncryptionHandler.Encrypt(Encoding.UTF8.GetBytes(ba.Town), aes)));
                command.Parameters.AddWithValue("@balance", ba.Balance);


                if (ba.GetType() == typeof(Current_Account))
                {
                    Current_Account ca = (Current_Account)ba;
                    command.Parameters.AddWithValue("@accountType", 1);
                    command.Parameters.AddWithValue("@overdraftAmount", ca.overdraftAmount);
                    command.Parameters.AddWithValue("@interestRate", DBNull.Value);
                }

                else
                {
                    Savings_Account sa = (Savings_Account)ba;
                    command.Parameters.AddWithValue("@accountType", 1);
                    command.Parameters.AddWithValue("@overdraftAmount", DBNull.Value);
                    command.Parameters.AddWithValue("@interestRate", sa.interestRate);
                }

                command.Parameters.AddWithValue("@iv", Convert.ToBase64String(iv)); // Save IV as a Base64 string



                // Calculate hash for integrity check
                string integrityHash = calculateIntegrityHash(ba);
                command.Parameters.AddWithValue("@integrity_hash", integrityHash);


                command.ExecuteNonQuery();


            }

            // Log information about the saved data
            EventLogger.WriteEvent($"Bank account data saved to the database. Account Detals: {ba}, User: {WindowsIdentity.GetCurrent().Name}, Timestamp: {DateTime.Now}", EventLogEntryType.Information, DateTime.Now);

            //grab the accoun number to clean the rest
            string bank_account_number = ba.AccountNo;
          
            // Clear sensitive information from memory after save to database 
            ba = null;
            GC.Collect();



            return bank_account_number;
        }

        private string calculateIntegrityHash(Bank_Account ba)
        {
            // Serialize the Bank_Account object to JSON
            string serializedData = JsonConvert.SerializeObject(ba);

            // Concatenate relevant columns for hashing
            string dataToHash = $"{ba.AccountNo}{ba.Name}{ba.Address_line_1}{ba.Address_line_2}{ba.Address_line_3}{ba.Town}{ba.Balance}{serializedData}";

            // Use a secure hashing algorithm (e.g., SHA-256)
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(dataToHash));
                return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
            }
        }

        private static byte[] generateRandomIV()
        {
            using (RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider())
            {
                byte[] iv = new byte[16]; // 128 bits for AES
                rng.GetBytes(iv);
                return iv;
            }
        }

        public bool CloseBankAccount(String encryptedAccNo)
        {

            // Log the event: Attempting to find bank account by account number
            String accNo = AesEncryptionHandler.EncryptAccountNumber(encryptedAccNo); // Decrypted data just to make log 
            EventLogger.WriteEvent($"Attempting to delete bank account by account number: {accNo}", EventLogEntryType.Information, DateTime.Now);
            // Set accNo to null after using it
            accNo = null;


            if (!File.Exists(Data_Access_Layer.databaseName))
                initialiseDatabase();

            if (encryptedAccNo == null)
                return false;
            else
            {
                using (var connection = getDatabaseConnection())
                {
                    connection.Open();
                    var command = connection.CreateCommand();
                    command.CommandText = "DELETE FROM Bank_Accounts WHERE accountNo = '" + encryptedAccNo + "'";
                    command.ExecuteNonQuery();

                }

                encryptedAccNo = null;
                GC.Collect();

                return true;
            }

        }

        public bool Lodge(String accNo, double amountToLodge)
        {
            // Get the call stack
            StackTrace stackTrace = new StackTrace();
            // Check if the caller is the main method or part of the system
            if (!IsSystemAndMainCaller(stackTrace))
            {
                // Log the event: Unauthorized caller
                EventLogger.WriteEvent($"Lodge failed. Unauthorized caller.", EventLogEntryType.Error, DateTime.Now);
                return false;
            }

            String encryptedAccNo = AesEncryptionHandler.EncryptAccountNumber(accNo);

         //   Bank_Account toLodgeTo = LoadBankAccountFromDatabase(accNo);
            Bank_Account toLodgeTo = LoadBankAccountFromDatabase(encryptedAccNo);


            if (toLodgeTo == null)
            {
                // Log the event: Account not found
                EventLogger.WriteEvent($"Lodge failed. Account not found: {accNo}.", EventLogEntryType.Error, DateTime.Now);

                // Log the stack trace
                LogStackTrace();

                return false;
            }
            else
            {
                // Perform withdrawal
                toLodgeTo.lodge(amountToLodge);

                // Update the Balance in the database
                UpdateAccountBalanceInDatabase(encryptedAccNo, toLodgeTo.Balance);

                // Get New Integraty Hash 
                string newIntegrityHash = calculateIntegrityHash(toLodgeTo);
                UpdateIntegrityHashInDatabase(encryptedAccNo, newIntegrityHash);

                // Clear sensitive information 
                toLodgeTo = null;
                newIntegrityHash = null;
                GC.Collect();

                return true;
            }
        }

        public Bank_Account FindBankAccountFromDatabaseWithOutDecryption(String accNo)
        {
            // Log the event: Attempting to find bank account by account number
            EventLogger.WriteEvent($"Attempting to find bank account by account number: {accNo}", EventLogEntryType.Information, DateTime.Now);

            using (var connection = getDatabaseConnection())
            {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText = "SELECT accountNo,name, address_line_1, address_line_2, address_line_3, town, balance, accountType, overdraftAmount, interestRate, iv FROM Bank_Accounts WHERE accountNo = @accountNo";
                command.Parameters.AddWithValue("@accountNo", AesEncryptionHandler.EncryptAccountNumber(accNo));

                SqliteDataReader dr = command.ExecuteReader();

                while (dr.Read())
                {
                    int accountType = dr.GetInt16(7);

                    if (accountType == Account_Type.Current_Account)
                    {
                        Current_Account ca = new Current_Account();

                        ca.AccountNo = dr.GetString(0);
                        ca.Name = dr.GetString(1);
                        ca.Address_line_1 = dr.GetString(2);
                        ca.Address_line_2 = dr.GetString(3);
                        ca.Address_line_3 = dr.GetString(4);
                        ca.Town = dr.GetString(5);
                        ca.Balance = dr.GetDouble(6);
                        ca.overdraftAmount = dr.GetDouble(8);

                        return ca;
                    }
                    else
                    {
                        Savings_Account sa = new Savings_Account();

                        sa.AccountNo = AesEncryptionHandler.DecryptAccountNumber(dr.GetString(0));
                        sa.Name = dr.GetString(1);
                        sa.Address_line_1 = dr.GetString(2);
                        sa.Address_line_2 = dr.GetString(3);
                        sa.Address_line_3 = dr.GetString(4);
                        sa.Town = dr.GetString(5);
                        sa.Balance = dr.GetDouble(6);
                        sa.interestRate = dr.GetDouble(9);

                        return sa;
                    }
                }
            }

            // Account not found in the database
            return null;
        }

        public void PrintBankAccountDetails(string accNo)
        {
            Bank_Account account = FindBankAccountByAccNo(accNo);

            if (account != null)
            {
                Type accountType = account.GetType();
                MethodInfo toStringMethod = accountType.GetMethod("ToString");

                if (toStringMethod != null)
                {
                    // Invoke the ToString method dynamically
                    string details = (string)toStringMethod.Invoke(account, null);

                    // Print the details
                    Console.WriteLine("Bank Account Details:");
                    Console.WriteLine(details);
                }
            }
            else
            {
                Console.WriteLine("Account Does Not Exist");
            }
        }

        public bool Withdraw(String accNo, double amountToWithdraw)
        {

            // Get the call stack
            StackTrace stackTrace = new StackTrace();
            // Check if the caller is the main method or part of the system
            if (!IsSystemAndMainCaller(stackTrace))
            {
                // Log the event: Unauthorized caller
                EventLogger.WriteEvent($"Withdrawal failed. Unauthorized caller.", EventLogEntryType.Error, DateTime.Now);
                return false;
            }


            // Bank_Account toWithdrawFrom = FindBankAccountFromDatabaseWithOutDecryption(accNo);

            String encryptedAccNo = AesEncryptionHandler.EncryptAccountNumber(accNo);

            Bank_Account toWithdrawFrom = LoadBankAccountFromDatabase(encryptedAccNo);


            if (toWithdrawFrom == null)
            {
                // Log the event: Account not found
                EventLogger.WriteEvent($"Withdrawal failed. Account not found: {accNo}.", EventLogEntryType.Error, DateTime.Now);

                // Log the stack trace
                LogStackTrace();

                return false;
            }

            // Check if withdrawal is possible based on the account type

            if (!toWithdrawFrom.CanWithdrawCC(amountToWithdraw))
            {
                // Log the event: Insufficient funds
                EventLogger.WriteEvent($"Withdrawal failed. Insufficient funds for account: {accNo}.", EventLogEntryType.Error, DateTime.Now);
                return false;
            }

            // Perform withdrawal
            bool result = toWithdrawFrom.withdraw(amountToWithdraw);

            if (!result)
            {
                // Log the event: Withdrawal failed
                EventLogger.WriteEvent($"Withdrawal failed for account: {accNo}.", EventLogEntryType.Error, DateTime.Now);
                return false;
            }

            //Update Balance to database 
            
            UpdateAccountBalanceInDatabase(encryptedAccNo, toWithdrawFrom.Balance);


            // Get New Integraty Hash 
            string newIntegrityHash = calculateIntegrityHash(toWithdrawFrom);
            UpdateIntegrityHashInDatabase(encryptedAccNo, newIntegrityHash);

            // Log the event: Withdrawal successful
            EventLogger.WriteEvent($"Withdrawal successful for account: {accNo}.", EventLogEntryType.Information, DateTime.Now);
            // Clear sensitive information

            toWithdrawFrom = null;
            GC.Collect();

            return true;
        }

        private void UpdateIntegrityHashInDatabase(string accountNo, string newIntegrityHash)
        {
            using (var connection = getDatabaseConnection())
            {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText = "UPDATE Bank_Accounts SET integrity_hash = @integrityHash WHERE accountNo = @accountNo";
                command.Parameters.AddWithValue("@integrityHash", newIntegrityHash);
                command.Parameters.AddWithValue("@accountNo", accountNo);
                command.ExecuteNonQuery();
            }
        }

        private void UpdateAccountBalanceInDatabase(string accountNo, double newBalance)
        {
            // Get the stack trace
            StackTrace stackTrace = new StackTrace();

            // Check if the caller is the main method or part of the system
            if (!IsAllowedCaller(stackTrace))
            {
                // Log the event: Unauthorized caller
                EventLogger.WriteEvent($"Update failed. Unauthorized caller.", EventLogEntryType.Error, DateTime.Now);
                LogStackTrace(); 

                return;
            }

            using (var connection = getDatabaseConnection())
            {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText = "UPDATE Bank_Accounts SET balance = @balance WHERE accountNo = @accountNo";
                command.Parameters.AddWithValue("@balance", newBalance);
                command.Parameters.AddWithValue("@accountNo", accountNo);
                command.ExecuteNonQuery();
            }
        }

        private bool IsAllowedCaller(StackTrace stackTrace)
        {
            // Get the calling method from the stack trace
            MethodBase callingMethod = stackTrace.GetFrame(1).GetMethod(); 

            // Check if the caller is one of the allowed methods
            return callingMethod?.Name == "Withdraw" || callingMethod?.Name == "Lodge";
        }

        private bool IsSystemAndMainCaller(StackTrace stackTrace)
        {
            MethodBase callingMethod = stackTrace.GetFrame(1).GetMethod();


            // Check if the caller is the main method or part of the system
            return stackTrace.GetFrame(1).GetMethod().Name == "Main" || callingMethod?.Name == "AddBankAccount";
        }

        private void LogStackTrace()
        {
            // Log the stack trace
            StackTrace stackTrace = new StackTrace(true);
            StackFrame[] stackFrames = stackTrace.GetFrames();

            // Log the stack trace
            if (stackFrames != null)
            {
                foreach (var frame in stackFrames)
                {
                    EventLogger.WriteEvent($"   at {frame.GetMethod()} in {frame.GetFileName()}:{frame.GetFileLineNumber()}", 
                    EventLogEntryType.Error, DateTime.Now);
                }
            }

        }

        private bool CheckAvailableDiskSpace(long requiredSpaceInBytes)
        {
            DriveInfo driveInfo = new DriveInfo(Path.GetPathRoot(AppDomain.CurrentDomain.BaseDirectory));
            long availableFreeSpace = driveInfo.AvailableFreeSpace;

            if (availableFreeSpace < requiredSpaceInBytes)
            {
                // Log the event: Insufficient free space
                EventLogger.WriteEvent($"Insufficient free space on the drive. Required: {requiredSpaceInBytes} bytes, Available: {availableFreeSpace} bytes", EventLogEntryType.Error, DateTime.Now);
                LogStackTrace(); // Log the stack trace
                return false;
            }

            return true;
        }


    }
}
