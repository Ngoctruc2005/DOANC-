SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRAN;

    DECLARE @Map TABLE
    (
        OldPoiId INT PRIMARY KEY,
        NewPoiId INT NOT NULL
    );

    INSERT INTO @Map (OldPoiId, NewPoiId)
    SELECT Poiid, ROW_NUMBER() OVER (ORDER BY Poiid)
    FROM POIs;

    IF (SELECT COUNT(*) FROM @Map) <> 7
    BEGIN
        THROW 50001, N'Số lượng quán hiện tại khác 7. Script này đang thiết kế để đặt lại ID từ 1 đến 7.', 1;
    END;

    ALTER TABLE Menu NOCHECK CONSTRAINT ALL;
    ALTER TABLE VisitLog NOCHECK CONSTRAINT ALL;
    ALTER TABLE POI_Categories NOCHECK CONSTRAINT ALL;

    -- Bước 1: Đổi tạm ID để tránh trùng khóa khi swap
    UPDATE p
    SET p.Poiid = p.Poiid + 1000
    FROM POIs p
    INNER JOIN @Map m ON m.OldPoiId = p.Poiid;

    UPDATE mnu
    SET mnu.Poiid = mnu.Poiid + 1000
    FROM Menu mnu
    INNER JOIN @Map m ON m.OldPoiId = mnu.Poiid;

    UPDATE v
    SET v.Poiid = v.Poiid + 1000
    FROM VisitLog v
    INNER JOIN @Map m ON m.OldPoiId = v.Poiid;

    UPDATE pc
    SET pc.Poiid = pc.Poiid + 1000
    FROM POI_Categories pc
    INNER JOIN @Map m ON m.OldPoiId = pc.Poiid;

    -- Bước 2: Đặt lại ID thật từ 1..7
    UPDATE p
    SET p.Poiid = m.NewPoiId
    FROM POIs p
    INNER JOIN @Map m ON p.Poiid = m.OldPoiId + 1000;

    UPDATE mnu
    SET mnu.Poiid = m.NewPoiId
    FROM Menu mnu
    INNER JOIN @Map m ON mnu.Poiid = m.OldPoiId + 1000;

    UPDATE v
    SET v.Poiid = m.NewPoiId
    FROM VisitLog v
    INNER JOIN @Map m ON v.Poiid = m.OldPoiId + 1000;

    UPDATE pc
    SET pc.Poiid = m.NewPoiId
    FROM POI_Categories pc
    INNER JOIN @Map m ON pc.Poiid = m.OldPoiId + 1000;

    ALTER TABLE Menu WITH CHECK CHECK CONSTRAINT ALL;
    ALTER TABLE VisitLog WITH CHECK CHECK CONSTRAINT ALL;
    ALTER TABLE POI_Categories WITH CHECK CHECK CONSTRAINT ALL;

    COMMIT TRAN;

    SELECT Poiid, Name
    FROM POIs
    ORDER BY Poiid;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRAN;

    DECLARE @ErrMsg NVARCHAR(4000) = ERROR_MESSAGE();
    DECLARE @ErrNo INT = ERROR_NUMBER();
    DECLARE @ErrState INT = ERROR_STATE();

    THROW @ErrNo, @ErrMsg, @ErrState;
END CATCH;
