SET SERVEROUTPUT ON SIZE UNLIMITED;

DECLARE
    v_sql   VARCHAR2(32767);
    v_count NUMBER;
    v_value VARCHAR2(4000) := 'HELLO WORLD!';
BEGIN
    FOR c IN (
        SELECT owner,
               table_name,
               column_name,
               data_type
        FROM all_tab_columns
        WHERE data_type IN (
            'VARCHAR2',
            'NVARCHAR2',
            'CHAR',
            'NCHAR',
            'CLOB',
            'NCLOB'
        )
        -- системные схемы обычно нет смысла проверять
        AND owner NOT IN (
            'SYS',
            'SYSTEM',
            'XDB',
            'MDSYS',
            'CTXSYS',
            'DBSNMP',
            'ORDSYS',
            'OUTLN'
        )
        ORDER BY owner, table_name, column_id
    )
    LOOP
        BEGIN
            IF c.data_type IN ('CLOB', 'NCLOB') THEN
                v_sql :=
                    'SELECT COUNT(*) FROM "' ||
                    REPLACE(c.owner, '"', '""') || '"."' ||
                    REPLACE(c.table_name, '"', '""') || '" WHERE DBMS_LOB.COMPARE("' ||
                    REPLACE(c.column_name, '"', '""') ||
                    '", TO_CLOB(:1)) = 0';
            ELSE
                v_sql :=
                    'SELECT COUNT(*) FROM "' ||
                    REPLACE(c.owner, '"', '""') || '"."' ||
                    REPLACE(c.table_name, '"', '""') || '" WHERE "' ||
                    REPLACE(c.column_name, '"', '""') ||
                    '" = :1';
            END IF;

            EXECUTE IMMEDIATE v_sql
                INTO v_count
                USING v_value;

            IF v_count > 0 THEN
                DBMS_OUTPUT.PUT_LINE(
                    'FOUND: ' ||
                    c.owner || '.' ||
                    c.table_name || '.' ||
                    c.column_name ||
                    '  ROWS=' || v_count
                );
            END IF;

        EXCEPTION
            WHEN OTHERS THEN
                -- Некоторые таблицы могут быть недоступны или иметь
                -- специфические ограничения. Просто продолжаем поиск.
                NULL;
        END;
    END LOOP;
END;
/
