
WITH tags AS (
    SELECT ROWNUM AS rn,
           COLUMN_VALUE AS tag
    FROM TABLE(
        SYS.ODCIVARCHAR2LIST(
            'CB01', 'CB02', 'CB03', 'CB04'
        )
    )
),
tag_values AS (
    SELECT
        t.rn,
        t.tag,
        MAX(
            CASE
                WHEN TRIM(a.MG_TG_VALUE) IS NULL
                  OR TRIM(a.MG_TG_VALUE) IN ('0', '?')
                THEN NULL
                ELSE a.MG_TG_VALUE
            END
        ) AS val
    FROM tags t
    LEFT JOIN YOUR_TABLE a
        ON a.MG_TG_TAG = t.tag
       AND a.MSBCODE = :P_MSBCODE
    GROUP BY t.rn, t.tag
)
SELECT
    '::FP_C_B' || CHR(10) ||
    LISTAGG(
        ':' || tag || ':' || val,
        CHR(10)
    ) WITHIN GROUP (ORDER BY rn)
    || CHR(10) || '::' AS RESULT
FROM tag_values;
