# README

A donation system written in C# that processes incoming payments from PayPal and saves them in a MySQL database.

## Installation

1. Copy the files anywhere on your hard drive.

2. Apply the schema file (see `bin/Schema.sql`) to your database. This creates a table
to store the donation/payments.


## Configuration

Edit your `config.json` with your credentials as follows:

Replace with your gameserver database credentials. Make sure to create a separate mysql
account for this app and grant it remote access.

Used to save the payments to the database. Privileges required include 
selecting and inserting rows.

````json
"mySql": {
  "connectionString": "server=HOST;database=DATABASENAME;uid=USERNAME;password=PASSWORD;charset=utf8;pooling=true;minimumpoolSize=2;maximumpoolsize=5;allow user variables=true"
}
````

Replace with your GMail account associated with your PayPal account on which the
payments are made.

Used in parsing transaction IDs from email notifications from PayPal.

````json
"gmail": {
  "host": "imap.googlemail.com",
  "port": 993,
  "username": "user@gmail.com",
  "password": "mypassword"
}
````

Your PayPal API access credentials. You can find them in your PayPal account 
under *Profile* -> *My Selling Tools* -> *API Access* -> *NVP API integration* ->
 *View API Signature*.

Required to validate the transaction IDs parsed earlier.

````json
"payPal": {
  "mode": "sandbox", // change to live 
  "username": "apiusername.gmail.com",
  "password": "password",
  "signature": "signature"
}
````

## Errors

Messages and errors are logged to the `logs` folder.