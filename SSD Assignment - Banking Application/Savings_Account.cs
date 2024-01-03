using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Banking_Application
{
    [Serializable]
    public sealed class Savings_Account: Bank_Account
    {

        public double interestRate;

       public Savings_Account(): base()
        {

        }
        
        public Savings_Account(String name, String address_line_1, String address_line_2, String address_line_3, String town, double balance, double interestRate) : base(name, address_line_1, address_line_2, address_line_3, town, balance)
        {
            this.interestRate = interestRate;
        }
        public override double getAvailableFunds()
        {
            return base.Balance;
        }

        public override bool CanWithdrawCC(double amountToWithdraw)
        {
            return Balance - amountToWithdraw >= 0;
        }

        public override bool withdraw(double amountToWithdraw)
        {
            double avFunds = getAvailableFunds();

            if (avFunds >= amountToWithdraw)
            {
                Balance -= amountToWithdraw;
                return true;
            }

            else
                return false;
        }

        public override String ToString()
        {

            return base.ToString() + 
                "Account Type: Savings Account\n" +
                "Interest Rate: " + interestRate + "\n";

        }


    }
}
