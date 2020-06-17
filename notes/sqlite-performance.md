
## Bucketed Object Table (256 individual tables for objects):

Test 1: 
Forgot to copy paste. 
200k total in db

Test 2:
-------------------------------------------------
BufferedWriteTest: Wrote 100000 in 19096 ms -> 5263.1578947368425 objects per second
-------------------------------------------------

-------------------------------------------------
BulkWriteMany: Wrote 100000 in 7401 ms -> 14285.714285714286 objects per second
-------------------------------------------------
400k total in db

Test 3:
-------------------------------------------------
BufferedWriteTest: Wrote 100000 in 22477 ms -> 4545.454545454545 objects per second
-------------------------------------------------

-------------------------------------------------
BulkWriteMany: Wrote 100000 in 8668 ms -> 12500 objects per second
-------------------------------------------------
600k total in db

Test 4: 
-------------------------------------------------
BufferedWriteTest: Wrote 100000 in 23438 ms -> 4347.826086956522 objects per second
-------------------------------------------------

-------------------------------------------------
BulkWriteMany: Wrote 100000 in 9288 ms -> 11111.111111111111 objects per second
-------------------------------------------------
800k total in db

Test 5: 
-------------------------------------------------
BufferedWriteTest: Wrote 100000 in 23735 ms -> 4347.826086956522 objects per second
-------------------------------------------------

-------------------------------------------------
BulkWriteMany: Wrote 100000 in 9944 ms -> 11111.111111111111 objects per second
-------------------------------------------------
1mil total in db