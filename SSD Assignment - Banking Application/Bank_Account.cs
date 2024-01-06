using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Banking_Application
{
    [Serializable]
    public abstract class Bank_Account
    {

        private string accountNo;
        private string name;
        private string address_line_1;
        private string address_line_2;
        private string address_line_3;
        private string town;
        private double balance;

        public string AccountNo
        {
            get { return accountNo; }
            set { accountNo = value; }
        }

        public string Name
        {
            get { return name; }
            set { name = value; }
        }
        public string Address_line_3
        {
            get { return address_line_3; }
            set { address_line_3 = value; }
        }

        public string Town
        {
            get { return town; }
            set { town = value; }
        }

        public double Balance
        {
            get { return balance; }
            set { balance = value; }
        }

        public string Address_line_1
        {
            get { return address_line_1; }
            set { address_line_1 = value; }
        }

        public string Address_line_2
        {
            get { return address_line_2; }
            set { address_line_2 = value; }
        }


        public Bank_Account()
        {
                        this.accountNo = System.Guid.NewGuid().ToString();

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

            Balance += amountIn;

        }

        public virtual bool CanWithdrawSaving(double amountToWithdraw)
        {
            return Balance - amountToWithdraw >= 0;
        }


        public abstract bool CanWithdrawCC(double amountToWithdraw);

        public abstract bool withdraw(double amountToWithdraw);

        public abstract double getAvailableFunds();

        public override String ToString()
        {

            return "\nAccount No: " + AccountNo + "\n" +
            "Name: " + Name + "\n" +
            "Address Line 1: " + Address_line_1 + "\n" +
            "Address Line 2: " + Address_line_2 + "\n" +
            "Address Line 3: " + Address_line_3 + "\n" +
            "Town: " + Town + "\n" +
            "Balance: " + Balance + "\n";

    }

    }
}
