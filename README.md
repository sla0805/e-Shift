# e-Shift
System Source Code

Setup Guide

## 1. Edit the Connection String
Open **`appsettings.json`** and update inside the `DefaultConnection` string to point to your local SQL Server instance.

Change to your own SQL Server setup in this place, "Server=YOUR_SERVER_NAME\\YOUR_INSTANCE_NAME".


## 2. Create and Apply Database Migrations
Run the following command in Package Manager Console:

> Update-Database


## 3. Run the Project


## Notes
There is an admin account initally setup in the database.
Email: superadmin@gmail.com
Password: Asd123!@#