﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Banking_Application
{
    [Serializable]
    public sealed class Current_Account: Bank_Account
    {

        public double overdraftAmount;

        public Current_Account(): base()
        {

        }
        
        public Current_Account(String name, String address_line_1, String address_line_2, String address_line_3, String town, double balance, double overdraftAmount) : base(name, address_line_1, address_line_2, address_line_3, town, balance)
        {
            this.overdraftAmount = overdraftAmount;
        }

        public override bool CanWithdrawCC(double amountToWithdraw)
        {
            double avFunds = getAvailableFunds();
            return amountToWithdraw <= avFunds;
        }

        public override bool withdraw(double amountToWithdraw)
        {
            double avFunds = getAvailableFunds();

            if (avFunds >= amountToWithdraw)
            {
                    Balance = avFunds-amountToWithdraw - overdraftAmount;

                return true;
            }

            else
                return false;

        }

        public override double getAvailableFunds()
        {
            return (base.Balance + overdraftAmount);
        }

        public override String ToString()
        {

            return base.ToString() +
                "Account Type: Current Account\n" +
                "Overdraft Amount: " + overdraftAmount + "\n";

        }

    }
}
