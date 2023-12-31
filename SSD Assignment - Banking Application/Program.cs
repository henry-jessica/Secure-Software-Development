using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;

namespace Banking_Application
{
    // Enums
    internal enum AccountType
    {
        CurrentAccount = 1,
        SavingsAccount = 2
    }

    public class Program
    {
        private static CpuUsageChecker cpuUsageChecker = new CpuUsageChecker();

        // The STAThread attribute is used to specify that the COM threading model for this application is single-threaded.
        [STAThread]
        public static void Main(string[] args)
        {

            Data_Access_Layer dal = Data_Access_Layer.getInstance();
            bool running = true;

            do
            {
                // Check CPU usage before executing each task
                float cpuUsage = cpuUsageChecker.GetCpuUsage();

                // Define a threshold for high CPU usage
                const float cpuThreshold = 80f; 

                // Check if CPU usage is above the threshold
                if (cpuUsage > cpuThreshold)
                {
                    Console.WriteLine($"High CPU usage ({cpuUsage}%). Task execution delayed.");
                }

                // Console.Clear(); 

                else
                {
                    Console.WriteLine("");
                    Console.WriteLine("***Banking Application Menu***");
                    Console.WriteLine("1. Add Bank Account");
                    Console.WriteLine("2. Close Bank Account");
                    Console.WriteLine("3. View Account Information");
                    Console.WriteLine("4. Make Lodgement");
                    Console.WriteLine("5. Make Withdrawal");
                    Console.WriteLine("6. Exit");
                    Console.WriteLine("CHOOSE OPTION:");
                    String option = Console.ReadLine();

                    switch (option)
                    {
                        case "1":
                            int loopCount = 0;
                            int accountType = 0; // Changed because now it is enum type

                            do
                            {

                                if (loopCount > 0)
                                    Console.WriteLine("INVALID OPTION CHOSEN - PLEASE TRY AGAIN");

                                Console.WriteLine("");
                                Console.WriteLine("***Account Types***:");
                                Console.WriteLine("1. Current Account.");
                                Console.WriteLine("2. Savings Account.");
                                Console.WriteLine("CHOOSE OPTION:");
                                accountType = Convert.ToInt32(Console.ReadLine());

                                loopCount++;
                            } while (!(accountType == (int)AccountType.CurrentAccount || accountType == (int)AccountType.SavingsAccount));


                            String name = "";
                            loopCount = 0;

                            do
                            {

                                if (loopCount > 0)
                                    Console.WriteLine("INVALID NAME ENTERED - PLEASE TRY AGAIN");

                                Console.WriteLine("Enter Name: ");
                                name = Console.ReadLine();

                                loopCount++;

                            } while (name.Equals(""));

                            String addressLine1 = "";
                            loopCount = 0;

                            do
                            {

                                if (loopCount > 0)
                                    Console.WriteLine("INVALID ÀDDRESS LINE 1 ENTERED - PLEASE TRY AGAIN");

                                Console.WriteLine("Enter Address Line 1: ");
                                addressLine1 = Console.ReadLine();

                                loopCount++;

                            } while (addressLine1.Equals(""));

                            Console.WriteLine("Enter Address Line 2: ");
                            String addressLine2 = Console.ReadLine();

                            Console.WriteLine("Enter Address Line 3: ");
                            String addressLine3 = Console.ReadLine();

                            String town = "";
                            loopCount = 0;

                            do
                            {

                                if (loopCount > 0)
                                    Console.WriteLine("INVALID TOWN ENTERED - PLEASE TRY AGAIN");

                                Console.WriteLine("Enter Town: ");
                                town = Console.ReadLine();

                                loopCount++;

                            } while (town.Equals(""));

                            double balance = -1;
                            loopCount = 0;

                            do
                            {

                                if (loopCount > 0)
                                    Console.WriteLine("INVALID OPENING BALANCE ENTERED - PLEASE TRY AGAIN");

                                Console.WriteLine("Enter Opening Balance: ");
                                String balanceString = Console.ReadLine();

                                try
                                {
                                    balance = Convert.ToDouble(balanceString);
                                }

                                catch
                                {
                                    loopCount++;
                                }

                            } while (balance < 0);

                            Bank_Account ba;

                            if (Convert.ToInt32(accountType) == Account_Type.Current_Account)
                            {
                                double overdraftAmount = -1;
                                loopCount = 0;

                                do
                                {

                                    if (loopCount > 0)
                                        Console.WriteLine("INVALID OVERDRAFT AMOUNT ENTERED - PLEASE TRY AGAIN");

                                    Console.WriteLine("Enter Overdraft Amount: ");
                                    String overdraftAmountString = Console.ReadLine();

                                    try
                                    {
                                        overdraftAmount = Convert.ToDouble(overdraftAmountString);
                                    }

                                    catch
                                    {
                                        loopCount++;
                                    }

                                } while (overdraftAmount < 0);

                                ba = new Current_Account(name, addressLine1, addressLine2, addressLine3, town, balance, overdraftAmount);
                            }

                            else
                            {

                                double interestRate = -1;
                                loopCount = 0;

                                do
                                {

                                    if (loopCount > 0)
                                        Console.WriteLine("INVALID INTEREST RATE ENTERED - PLEASE TRY AGAIN");

                                    Console.WriteLine("Enter Interest Rate: ");
                                    String interestRateString = Console.ReadLine();

                                    try
                                    {
                                        interestRate = Convert.ToDouble(interestRateString);
                                    }

                                    catch
                                    {
                                        loopCount++;
                                    }

                                } while (interestRate < 0);

                                ba = new Savings_Account(name, addressLine1, addressLine2, addressLine3, town, balance, interestRate);
                            }

                            String accNo = dal.AddBankAccount(ba);

                            Console.WriteLine("New Account Number Is: " + accNo);

                            // Clean 
                            accNo = null;
                            GC.Collect();


                            break;
                        case "2":
                            Console.WriteLine("Enter Account Number: ");
                            accNo = Console.ReadLine();

                            ba = dal.FindBankAccountFromDatabaseWithOutDecryption(accNo);

                            if (ba is null)
                            {
                                Console.WriteLine("Account Does Not Exist");
                            }
                            else if (ba.balance > 0)
                            {
                                Console.WriteLine("Account has a positive balance €{0:0.00}. Please withdraw the funds before closing the account.", ba.balance);
                            }
                            else if (ba.balance < 0)
                            {
                                Console.WriteLine("Cannot close the account. The account balance is negative (€{0:0.00}). Please make a lodgement to bring the balance to positive before closing the account.", Math.Abs(ba.balance));
                            }
                            else
                            {

                                Bank_Account ba_details = dal.FindBankAccountByAccNo(accNo);
                                Console.WriteLine(ba_details.ToString());
                                
                                // clean after use it 
                                
                                ba_details = null;
                                GC.Collect();

                                String ans = "";

                                do
                                {

                                    Console.WriteLine("Proceed With Delection (Y/N)?");
                                    ans = Console.ReadLine();

                                    switch (ans)
                                    {
                                        case "Y":
                                        case "y":
                                            dal.CloseBankAccount(ba.accountNo); //encrypted bank account number 
                                            Console.WriteLine("Account successfully deleted.");
                                            break;
                                        case "N":
                                        case "n":
                                            break;
                                        default:
                                            Console.WriteLine("INVALID OPTION CHOSEN - PLEASE TRY AGAIN");
                                            break;
                                    }
                                } while (!(ans.Equals("Y") || ans.Equals("y") || ans.Equals("N") || ans.Equals("n")));
                            }

                            break;
                        case "3":
                            Console.WriteLine("Enter Account Number: ");
                            accNo = Console.ReadLine();
                            dal.PrintBankAccountDetails(accNo);

                            break;
                        case "4": //Lodge
                            Console.WriteLine("Enter Account Number: ");
                            accNo = Console.ReadLine();

                            ba = dal.FindBankAccountByAccNo(accNo);

                            if (ba is null)
                            {
                                Console.WriteLine("Account Does Not Exist");
                            }
                            else
                            {
                                double amountToLodge = -1;
                                loopCount = 0;

                                do
                                {

                                    if (loopCount > 0)
                                        Console.WriteLine("INVALID AMOUNT ENTERED - PLEASE TRY AGAIN");

                                    Console.WriteLine("Enter Amount To Lodge: ");
                                    String amountToLodgeString = Console.ReadLine();

                                    try
                                    {
                                        amountToLodge = Convert.ToDouble(amountToLodgeString);
                                    }

                                    catch
                                    {
                                        loopCount++;
                                    }

                                } while (amountToLodge < 0);

                                dal.Lodge(accNo, amountToLodge);
                            }
                            break;
                        case "5": //Withdraw
                            Console.WriteLine("Enter Account Number: ");
                            accNo = Console.ReadLine();

                            ba = dal.FindBankAccountByAccNo(accNo);

                            if (ba is null)
                            {
                                Console.WriteLine("Account Does Not Exist");
                            }
                            else
                            {
                                double amountToWithdraw = -1;
                                loopCount = 0;

                                do
                                {

                                    if (loopCount > 0)
                                        Console.WriteLine("INVALID AMOUNT ENTERED - PLEASE TRY AGAIN");

                                    Console.WriteLine("Enter Amount To Withdraw (€" + ba.getAvailableFunds() + " Available): ");
                                    String amountToWithdrawString = Console.ReadLine();

                                    try
                                    {
                                        amountToWithdraw = Convert.ToDouble(amountToWithdrawString);
                                    }

                                    catch
                                    {
                                        loopCount++;
                                    }

                                } while (amountToWithdraw < 0);

                                bool withdrawalOK = dal.Withdraw(accNo, amountToWithdraw);

                                if (withdrawalOK == false)
                                {

                                    Console.WriteLine("Insufficient Funds Available.");
                                }
                                else
                                {
                                    Console.WriteLine("Success - Take your money.");
                                }
                            }
                            break;
                        case "6":
                            running = false;
                            break;
                        default:
                            Console.WriteLine("INVALID OPTION CHOSEN - PLEASE TRY AGAIN");
                            break;
                    }

                }
            } while (running != false);


        }

    }
}