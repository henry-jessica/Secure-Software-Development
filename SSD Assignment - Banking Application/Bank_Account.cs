﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Banking_Application
{
    public abstract class Bank_Account
    {

        public String accountNo;
        public String name;
        public String address_line_1;
        public String address_line_2;
        public String address_line_3;
        public String town;
        public double balance;
        public string iv;

        public Bank_Account()
        {
        }

        public Bank_Account(String name, String address_line_1, String address_line_2, String address_line_3, String town, double balance)
        {
            this.name = name;
            this.address_line_1 = address_line_1;
            this.address_line_2 = address_line_2;
            this.address_line_3 = address_line_3;
            this.town = town;
            this.balance = balance;
            this.accountNo = System.Guid.NewGuid().ToString();

        }

        public void lodge(double amountIn)
        {

            balance += amountIn;

        }

        public virtual bool CanWithdrawSaving(double amountToWithdraw)
        {
            return balance - amountToWithdraw >= 0;
        }


        public abstract bool CanWithdrawCC(double amountToWithdraw);

        public abstract bool withdraw(double amountToWithdraw);

        public abstract double getAvailableFunds();

        public override String ToString()
        {

            return "\nAccount No: " + accountNo + "\n" +
            "Name: " + name + "\n" +
            "Address Line 1: " + address_line_1 + "\n" +
            "Address Line 2: " + address_line_2 + "\n" +
            "Address Line 3: " + address_line_3 + "\n" +
            "Town: " + town + "\n" +
            "Balance: " + balance + "\n";

    }

    }
}
