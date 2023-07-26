#ACMEv2 Client library

https://tools.ietf.org/html/draft-ietf-acme-acme-18

The following diagram illustrates the relations between resources on
an ACME server.  For the most part, these relations are expressed by
URLs provided as strings in the resources' JSON representations.
Lines with labels in quotes indicate HTTP link relations.

                                directory
                                    |
                                    +--> new-nonce
                                    |
        +----------+----------+-----+-----+------------+
        |          |          |           |            |
        |          |          |           |            |
        V          V          V           V            V
    newAccount   newAuthz   newOrder   revokeCert   keyChange
        |          |          |
        |          |          |
        V          |          V
    account       |        order -----> cert
                    |          |
                    |          |
                    |          V
                    +------> authz
                            | ^
                            | | "up"
                            V |
                            challenge
  
  
  
  +-------------------+--------------------------------+--------------+
   | Action            | Request                        | Response     |
   +-------------------+--------------------------------+--------------+
   | Get directory     | GET  directory                 | 200          |
   |                   |                                |              |
   | Get nonce         | HEAD newNonce                  | 200          |
   |                   |                                |              |
   | Create account    | POST newAccount                | 201 ->       |
   |                   |                                | account      |
   |                   |                                |              |
   | Submit order      | POST newOrder                  | 201 -> order |
   |                   |                                |              |
   | Fetch challenges  | POST-as-GET order's            | 200          |
   |                   | authorization urls             |              |
   |                   |                                |              |
   | Respond to        | POST authorization challenge   | 200          |
   | challenges        | urls                           |              |
   |                   |                                |              |
   | Poll for status   | POST-as-GET order              | 200          |
   |                   |                                |              |
   | Finalize order    | POST order's finalize url      | 200          |
   |                   |                                |              |
   | Poll for status   | POST-as-GET order              | 200          |
   |                   |                                |              |
   | Download          | POST-as-GET order's            | 200          |
   | certificate       | certificate url                |              |
   +-------------------+--------------------------------+--------------+






            pending
               |
               | Receive
               | response
               V
           processing <-+
               |   |    | Server retry or
               |   |    | client retry request
               |   +----+
               |
               |
   Successful  |   Failed
   validation  |   validation
     +---------+---------+
     |                   |
     V                   V
   valid              invalid




   https://community.letsencrypt.org/t/acme-client-finalized-order-stuck-on-ready-state/165196

   The following table illustrates a typical sequence of requests
   required to establish a new account with the server, prove control of
   an identifier, issue a certificate, and fetch an updated certificate
   some time after issuance.  The "->" is a mnemonic for a Location
   header field pointing to a created resource.

   +-------------------+--------------------------------+--------------+
   | Action            | Request                        | Response     |
   +-------------------+--------------------------------+--------------+
   | Get directory     | GET  directory                 | 200          |
   |                   |                                |              |
   | Get nonce         | HEAD newNonce                  | 200          |
   |                   |                                |              |
   | Create account    | POST newAccount                | 201 ->       |
   |                   |                                | account      |
   |                   |                                |              |
   | Submit order      | POST newOrder                  | 201 -> order |
   |                   |                                |              |
   | Fetch challenges  | POST-as-GET order's            | 200          |
   |                   | authorization urls             |              |
   |                   |                                |              |
   | Respond to        | POST authorization challenge   | 200          |
   | challenges        | urls                           |              |
   |                   |                                |              |
   | Poll for status   | POST-as-GET order              | 200          |
   |                   |                                |              |
   | Finalize order    | POST order's finalize url      | 200          |
   |                   |                                |              |
   | Poll for status   | POST-as-GET order              | 200          |
   |                   |                                |              |
   | Download          | POST-as-GET order's            | 200          |
   | certificate       | certificate url                |              |
   +-------------------+--------------------------------+--------------+