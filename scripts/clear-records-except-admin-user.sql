-- Clears every base table in the selected MySQL database except admin_user.
-- PowerShell:
-- .\scripts\clear-records-except-admin-user.ps1
--
-- cmd.exe:
-- mysql -u root -p site_management < scripts\clear-records-except-admin-user.sql

DELIMITER //

DROP PROCEDURE IF EXISTS clear_records_except_admin_user//

CREATE PROCEDURE clear_records_except_admin_user()
BEGIN
    DECLARE done INT DEFAULT FALSE;
    DECLARE table_name_value VARCHAR(255);

    DECLARE table_cursor CURSOR FOR
        SELECT table_name
        FROM information_schema.tables
        WHERE table_schema = DATABASE()
            AND table_type = 'BASE TABLE'
            AND table_name <> 'admin_user'
        ORDER BY table_name;

    DECLARE CONTINUE HANDLER FOR NOT FOUND SET done = TRUE;
    DECLARE EXIT HANDLER FOR SQLEXCEPTION
    BEGIN
        SET FOREIGN_KEY_CHECKS = 1;
        RESIGNAL;
    END;

    IF DATABASE() IS NULL THEN
        SIGNAL SQLSTATE '45000'
            SET MESSAGE_TEXT = 'Select a database before running this cleanup script.';
    END IF;

    SET FOREIGN_KEY_CHECKS = 0;

    OPEN table_cursor;

    read_loop: LOOP
        FETCH table_cursor INTO table_name_value;
        IF done THEN
            LEAVE read_loop;
        END IF;

        SET @delete_sql = CONCAT('DELETE FROM `', REPLACE(table_name_value, '`', '``'), '`');
        PREPARE delete_statement FROM @delete_sql;
        EXECUTE delete_statement;
        DEALLOCATE PREPARE delete_statement;
    END LOOP;

    CLOSE table_cursor;

    SET FOREIGN_KEY_CHECKS = 1;
END//

DELIMITER ;

CALL clear_records_except_admin_user();
DROP PROCEDURE clear_records_except_admin_user;
