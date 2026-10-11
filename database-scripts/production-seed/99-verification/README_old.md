# 99 — final read-only verification

`001-verify-canonical-seed.sql` runs inside a transaction and rolls back after checking the result.
It fails unless exactly Deepesh Gupta and Sujeet are the retained Host users, Tenant and Employee are
empty, the Host permission baseline exists, and the four-country geography catalogue is present.
