﻿       IDENTIFICATION DIVISION.
       PROGRAM-ID. IFTHEN.
      
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A PIC 9(2) VALUE 10.
       PROCEDURE DIVISION.
           IF A = 10 THEN
             DISPLAY "A = 10"
             DISPLAY "RIGHT ?"
           ELSE
             DISPLAY "A <> 10"
             DISPLAY "???"
           END-IF.
       END PROGRAM IFTHEN.
      