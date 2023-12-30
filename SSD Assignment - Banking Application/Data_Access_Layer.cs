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


namespace Banking_Application
{
    public class Data_Access_Layer
    {

        private List<Bank_Account> accounts;
        private static String databaseName = "Banking Database.db";

        // provide a thread-safe way to implement lazy initialization
        private static readonly Lazy<Data_Access_Layer> lazyInstance =  new Lazy<Data_Access_Layer>(() => new Data_Access_Layer());

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
            Logger.WriteEvent($"Attempting to find bank account by account number: {accNo}", EventLogEntryType.Information, DateTime.Now);

            String encryptedAccNo = AesEncryptionHandler.EncryptAccountNumber(accNo);

            Bank_Account ba = LoadBankAccountFromDatabase(encryptedAccNo);

            if (ba == null)
                return null;
            else
                return ba;
        }

        public Bank_Account LoadBankAccountFromDatabase(String encryptedAccNo)
        {
            // Ensure the database is initialized
            if (!File.Exists(Data_Access_Layer.databaseName))
                initialiseDatabase();

            // Check if the account is already loaded in the accounts list
            Bank_Account existingAccount = accounts.FirstOrDefault(acc => acc.accountNo.Equals(encryptedAccNo, StringComparison.OrdinalIgnoreCase));

            if (existingAccount != null)
            {
                return existingAccount;
            }

            // If the account is not already loaded, load it from the database
            using (var connection = getDatabaseConnection())
            {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText = "SELECT * FROM Bank_Accounts WHERE accountNo = @accountNo";
                command.Parameters.AddWithValue("@accountNo", encryptedAccNo);

                SqliteDataReader dr = command.ExecuteReader();

                while (dr.Read())
                {
                    int accountType = dr.GetInt16(7);

                    if (accountType == Account_Type.Current_Account)
                    {
                        // Get from database the encrypted data 
                        byte[] encryptedNameBytes = Convert.FromBase64String(dr.GetString(1));
                        byte[] encryptedAddressLine1Bytes = Convert.FromBase64String(dr.GetString(2));
                        byte[] encryptedAddressLine2Bytes = Convert.FromBase64String(dr.GetString(3));
                        byte[] encryptedAddressLine3Bytes = Convert.FromBase64String(dr.GetString(4));
                        byte[] encryptedTownBytes = Convert.FromBase64String(dr.GetString(5));


                        string ivBase64 = dr.GetString(10); // Read IV as Base64 string from the database
                        byte[] iv = Convert.FromBase64String(ivBase64); // Convert IV to byte array for decryption

                        Current_Account ca = new Current_Account();

                        ca.accountNo = AesEncryptionHandler.DecryptAccountNumber(dr.GetString(0));
                        ca.name = Encoding.UTF8.GetString(AesEncryptionHandler.Decrypt(encryptedNameBytes, AesEncryptionHandler.GetOrCreateAesEncryptionKeyCBC(iv)));
                        ca.address_line_1 = Encoding.UTF8.GetString(AesEncryptionHandler.Decrypt(encryptedAddressLine1Bytes, AesEncryptionHandler.GetOrCreateAesEncryptionKeyCBC(iv)));
                        ca.address_line_2 = Encoding.UTF8.GetString(AesEncryptionHandler.Decrypt(encryptedAddressLine2Bytes, AesEncryptionHandler.GetOrCreateAesEncryptionKeyCBC(iv)));
                        ca.address_line_3 = Encoding.UTF8.GetString(AesEncryptionHandler.Decrypt(encryptedAddressLine3Bytes, AesEncryptionHandler.GetOrCreateAesEncryptionKeyCBC(iv)));
                        ca.town = Encoding.UTF8.GetString(AesEncryptionHandler.Decrypt(encryptedTownBytes, AesEncryptionHandler.GetOrCreateAesEncryptionKeyCBC(iv)));
                        ca.balance = dr.GetDouble(6);
                        ca.overdraftAmount = dr.GetDouble(8);

                        return ca;
                    }
                    else
                    {

                        // Get from database the encrypted data 
                        byte[] encryptedNameBytes = Convert.FromBase64String(dr.GetString(1));
                        byte[] encryptedAddressLine1Bytes = Convert.FromBase64String(dr.GetString(2));
                        byte[] encryptedAddressLine2Bytes = Convert.FromBase64String(dr.GetString(3));
                        byte[] encryptedAddressLine3Bytes = Convert.FromBase64String(dr.GetString(4));
                        byte[] encryptedTownBytes = Convert.FromBase64String(dr.GetString(5));

                        string ivBase64 = dr.GetString(10); // Read IV as Base64 string from the database

                        byte[] iv = Convert.FromBase64String(ivBase64); // Convert IV to byte array for decryption


                        Savings_Account sa = new Savings_Account();

                        sa.accountNo = AesEncryptionHandler.DecryptAccountNumber(dr.GetString(0));
                        sa.name = Encoding.UTF8.GetString(AesEncryptionHandler.Decrypt(encryptedNameBytes, AesEncryptionHandler.GetOrCreateAesEncryptionKeyCBC(iv)));
                        sa.address_line_1 = Encoding.UTF8.GetString(AesEncryptionHandler.Decrypt(encryptedAddressLine1Bytes, AesEncryptionHandler.GetOrCreateAesEncryptionKeyCBC(iv)));
                        sa.address_line_2 = Encoding.UTF8.GetString(AesEncryptionHandler.Decrypt(encryptedAddressLine2Bytes, AesEncryptionHandler.GetOrCreateAesEncryptionKeyCBC(iv)));
                        sa.address_line_3 = Encoding.UTF8.GetString(AesEncryptionHandler.Decrypt(encryptedAddressLine3Bytes, AesEncryptionHandler.GetOrCreateAesEncryptionKeyCBC(iv)));
                        sa.town = Encoding.UTF8.GetString(AesEncryptionHandler.Decrypt(encryptedTownBytes, AesEncryptionHandler.GetOrCreateAesEncryptionKeyCBC(iv)));
                        sa.balance = dr.GetDouble(6);
                        sa.interestRate = dr.GetDouble(9);
                        return sa;
                    }
                }
            }

            // Account not found in the database
            return null;
        }

        public String AddBankAccount(Bank_Account ba)
        {
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

                // Calculate hash for integrity check
                string integrityHash = calculateIntegrityHash(ba);
                command.Parameters.AddWithValue("@integrity_hash", integrityHash);

                byte[] iv = generateRandomIV();
                Aes aes = AesEncryptionHandler.GetOrCreateAesEncryptionKeyCBC(iv);

                // Add parameters directly to the command's Parameters collection
                // command.Parameters.AddWithValue("@accountNo", ba.accountNo);
                command.Parameters.AddWithValue("@accountNo",(AesEncryptionHandler.EncryptAccountNumber(ba.accountNo)));
                command.Parameters.AddWithValue("@name",Convert.ToBase64String(AesEncryptionHandler.Encrypt(Encoding.UTF8.GetBytes(ba.name), aes)));
                command.Parameters.AddWithValue("@address_line_1", Convert.ToBase64String(AesEncryptionHandler.Encrypt(Encoding.UTF8.GetBytes(ba.address_line_1), aes)));
                command.Parameters.AddWithValue("@address_line_2", Convert.ToBase64String(AesEncryptionHandler.Encrypt(Encoding.UTF8.GetBytes(ba.address_line_2), aes)));
                command.Parameters.AddWithValue("@address_line_3", Convert.ToBase64String(AesEncryptionHandler.Encrypt(Encoding.UTF8.GetBytes(ba.address_line_3), aes)));
                command.Parameters.AddWithValue("@town", Convert.ToBase64String(AesEncryptionHandler.Encrypt(Encoding.UTF8.GetBytes(ba.town), aes)));
                command.Parameters.AddWithValue("@balance", ba.balance);


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


                command.ExecuteNonQuery();

            }

            //grab the accoun number to clean the rest
            string bank_account_number = ba.accountNo;

            // Clear sensitive information from memory after save to database 
            ba = null;
            GC.Collect();

            return bank_account_number;
        }

        private string calculateIntegrityHash(Bank_Account ba)
        {
            // Concatenate relevant columns for hashing
            string dataToHash = $"{ba.accountNo}{ba.name}{ba.address_line_1}{ba.address_line_2}{ba.address_line_3}{ba.town}{ba.balance}";

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
            Logger.WriteEvent($"Attempting to delete bank account by account number: {accNo}", EventLogEntryType.Information, DateTime.Now);
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

            Bank_Account toLodgeTo = null;

            foreach (Bank_Account ba in accounts)
            {

                if (ba.accountNo.Equals(accNo))
                {
                    ba.lodge(amountToLodge);
                    toLodgeTo = ba;
                    break;
                }

            }

            if (toLodgeTo == null)
                return false;
            else
            {

                using (var connection = getDatabaseConnection())
                {
                    connection.Open();
                    var command = connection.CreateCommand();
                    command.CommandText = "UPDATE Bank_Accounts SET balance = @balance WHERE accountNo = @accountNo";
                    command.Parameters.AddWithValue("@balance", toLodgeTo.balance);
                    command.Parameters.AddWithValue("@accountNo", toLodgeTo.accountNo);
                    command.ExecuteNonQuery();
                }

                // Clear sensitive information 
                toLodgeTo = null;
                GC.Collect();

                return true;
            }

        }

        public Bank_Account FindBankAccountFromDatabaseWithOutDecryption(String accNo)
        {
            // Log the event: Attempting to find bank account by account number
            Logger.WriteEvent($"Attempting to find bank account by account number: {accNo}", EventLogEntryType.Information, DateTime.Now);

            using (var connection = getDatabaseConnection())
            {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText = "SELECT * FROM Bank_Accounts WHERE accountNo = @accountNo";
                command.Parameters.AddWithValue("@accountNo", AesEncryptionHandler.EncryptAccountNumber(accNo));

                SqliteDataReader dr = command.ExecuteReader();

                while (dr.Read())
                {
                    int accountType = dr.GetInt16(7);

                    if (accountType == Account_Type.Current_Account)
                    {
                        Current_Account ca = new Current_Account();

                        ca.accountNo = dr.GetString(0);
                        ca.name = dr.GetString(1);
                        ca.address_line_1 = dr.GetString(2);
                        ca.address_line_2 = dr.GetString(3);
                        ca.address_line_3 = dr.GetString(4);
                        ca.town = dr.GetString(5);
                        ca.balance = dr.GetDouble(6);
                        ca.overdraftAmount = dr.GetDouble(8);

                        return ca;
                    }
                    else
                    {
                        Savings_Account sa = new Savings_Account();

                        sa.accountNo = AesEncryptionHandler.DecryptAccountNumber(dr.GetString(0));
                        sa.name = dr.GetString(1);
                        sa.address_line_1 = dr.GetString(2);
                        sa.address_line_2 = dr.GetString(3);
                        sa.address_line_3 = dr.GetString(4);
                        sa.town = dr.GetString(5);
                        sa.balance = dr.GetDouble(6);
                        sa.interestRate = dr.GetDouble(9);

                        return sa;
                    }
                }
            }

            // Account not found in the database
            return null;
        }

        // HERE I AM APPLYING SOME REFLACTION

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

            Bank_Account toWithdrawFrom = null;
            bool result = false;

            foreach (Bank_Account ba in accounts)
            {

                if (ba.accountNo.Equals(accNo))
                {
                    result = ba.withdraw(amountToWithdraw);
                    toWithdrawFrom = ba;
                    break;
                }

            }

            if (toWithdrawFrom == null || result == false)
                return false;
            else
            {

                using (var connection = getDatabaseConnection())
                {
                    connection.Open();
                    var command = connection.CreateCommand();

                    command.CommandText = "UPDATE Bank_Accounts SET balance = @balance WHERE accountNo = @accountNo";
                    command.Parameters.AddWithValue("@balance", toWithdrawFrom.balance);
                    command.Parameters.AddWithValue("@accountNo", toWithdrawFrom.accountNo);
                    command.ExecuteNonQuery();
                }

                toWithdrawFrom = null;
                GC.Collect();

                return true;
            }

        }

    }
}
