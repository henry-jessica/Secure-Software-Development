using System;
using System.Buffers.Text;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;

namespace Banking_Application
{
    public class Data_Access_Layer
    {

        private List<Bank_Account> accounts;
        public static String databaseName = "Banking Database.db";
        private static Data_Access_Layer instance = new Data_Access_Layer();

        private Data_Access_Layer()//Singleton Design Pattern (For Concurrency Control) - Use getInstance() Method Instead.
        {
            // Initialize the AesEncrypti
            accounts = new List<Bank_Account>();
        }

        public static Data_Access_Layer getInstance()
        {
            return instance;
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
      
        public Bank_Account findBankAccountByAccNo(String accNo)
        {
            initialiseDatabase();

            // Log the event: Attempting to find bank account by account number
           // Logger.LogEvent("Attempting to find bank account", $"Account Number: {accNo}");
            Logger.WriteEvent($"Attempting to find bank account by account number: {accNo}", EventLogEntryType.Information, DateTime.Now);


            // Check if the account is already loaded in the accounts list
            Bank_Account existingAccount = accounts.FirstOrDefault(acc => acc.accountNo.Equals(accNo, StringComparison.OrdinalIgnoreCase));

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
                command.Parameters.AddWithValue("@accountNo", accNo);

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
                        ca.accountNo = dr.GetString(0);
                        ca.name = Encoding.UTF8.GetString(AesEncryptionHendler.Decrypt(encryptedNameBytes, AesEncryptionHendler.GetOrCreateAesEncryptionKey(iv)));
                        ca.address_line_1 = Encoding.UTF8.GetString(AesEncryptionHendler.Decrypt(encryptedAddressLine1Bytes, AesEncryptionHendler.GetOrCreateAesEncryptionKey(iv)));
                        ca.address_line_2 = Encoding.UTF8.GetString(AesEncryptionHendler.Decrypt(encryptedAddressLine2Bytes, AesEncryptionHendler.GetOrCreateAesEncryptionKey(iv)));
                        ca.address_line_3 = Encoding.UTF8.GetString(AesEncryptionHendler.Decrypt(encryptedAddressLine3Bytes, AesEncryptionHendler.GetOrCreateAesEncryptionKey(iv)));
                        ca.town = Encoding.UTF8.GetString(AesEncryptionHendler.Decrypt(encryptedTownBytes, AesEncryptionHendler.GetOrCreateAesEncryptionKey(iv)));
                        ca.balance = dr.GetDouble(6);
                        ca.overdraftAmount = dr.GetDouble(8);

                        accounts.Add(ca);
                        return ca;
                    }
                    else
                    {
                        Savings_Account sa = new Savings_Account();
                        // ... (existing code to populate Savings_Account)
                        accounts.Add(sa);
                        return sa;
                    }
                }
            }

            // Account not found in the database
            return null;
        }

        public String addBankAccount(Bank_Account ba)
        {
            // Ensure the database is initialized
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
                string integrityHash = CalculateIntegrityHash(ba);
                command.Parameters.AddWithValue("@integrity_hash", integrityHash);

                byte[] iv = GenerateRandomIV();
                Aes aes = AesEncryptionHendler.GetOrCreateAesEncryptionKey(iv);

                // Add parameters directly to the command's Parameters collection
                command.Parameters.AddWithValue("@accountNo", ba.accountNo);
                command.Parameters.AddWithValue("@name",Convert.ToBase64String(AesEncryptionHendler.Encrypt(Encoding.UTF8.GetBytes(ba.name), aes)));
                command.Parameters.AddWithValue("@address_line_1", Convert.ToBase64String(AesEncryptionHendler.Encrypt(Encoding.UTF8.GetBytes(ba.address_line_1), aes)));
                command.Parameters.AddWithValue("@address_line_2", Convert.ToBase64String(AesEncryptionHendler.Encrypt(Encoding.UTF8.GetBytes(ba.address_line_2), aes)));
                command.Parameters.AddWithValue("@address_line_3", Convert.ToBase64String(AesEncryptionHendler.Encrypt(Encoding.UTF8.GetBytes(ba.address_line_3), aes)));
                command.Parameters.AddWithValue("@town", Convert.ToBase64String(AesEncryptionHendler.Encrypt(Encoding.UTF8.GetBytes(ba.town), aes)));
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

            return ba.accountNo;
        }

        private string CalculateIntegrityHash(Bank_Account ba)
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



        //TODO review
        public static byte[] GenerateRandomIV()
        {
            using (RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider())
            {
                byte[] iv = new byte[16]; // 128 bits for AES
                rng.GetBytes(iv);
                return iv;
            }
        }


        public Bank_Account findBankAccountByAccNo2(String accNo) 
        { 
        
            foreach(Bank_Account ba in accounts)
            {

                if (ba.accountNo.Equals(accNo))
                {
                    return ba;
                }

            }

            return null; 
        }

        public bool closeBankAccount(String accNo) 
        {

            Bank_Account toRemove = null;
            
            foreach (Bank_Account ba in accounts)
            {

                if (ba.accountNo.Equals(accNo))
                {
                    toRemove = ba;
                    break;
                }

            }

            if (toRemove == null)
                return false;
            else
            {
                accounts.Remove(toRemove);

                using (var connection = getDatabaseConnection())
                {
                    connection.Open();
                    var command = connection.CreateCommand();
                    command.CommandText = "DELETE FROM Bank_Accounts WHERE accountNo = '" + toRemove.accountNo + "'";
                    command.ExecuteNonQuery();

                }

                return true;
            }

        }

        public bool lodge(String accNo, double amountToLodge)
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

                return true;
            }

        }

        public bool withdraw(String accNo, double amountToWithdraw)
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

                return true;
            }

        }

    }
}
