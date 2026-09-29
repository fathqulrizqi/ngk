$(document).ready(function () {
    var period = parseInt($("#selLCWorkingDayPeriod").val(),10);
    var cHeader = ['Category'];
    var cwidth = [125];
    var currDate = new Date(period, 9, 1);
    var lastDate = new Date(period + 4, 3, 1);
    var mDiff = monthDiff(currDate, lastDate);
    for (var i = 0; i <= mDiff; i++) {
        cHeader.push(currDate.getFullYear() + "_" + ((currDate.getMonth() * 1) + 1));
        cwidth.push(100);
        currDate.setMonth(currDate.getMonth() + 1);
    }

    $('#divWorkingDay').jexcel({
        url: "/NGKBusi/FA/LaborCost/GetWorkingDay?iLCWorkingDayPeriod=" + period,
        colHeaders: cHeader,
        colWidths: cwidth,
        allowInsertRow: false,
        // Allow new rows
        allowManualInsertRow: false,
        // Allow new columns
        allowInsertColumn: false,
        // Allow new rows
        allowManualInsertColumn: false,
        // Allow row delete
        allowDeleteRow: false,
        // Allow column delete
        allowDeleteColumn: false,
        columns: [
            { readOnly: true}
        ]
        
    });
    $("#selLCWorkingDayPeriod").change(function () {
        $("#btnLCWorkingDay").click();
    });


    $('#btnLCWorkingDaySave').click(function () {
        $("#divWorkingDay").LoadingOverlay("show");
        var currData = $('#divWorkingDay').jexcel('getData');
        var currDataArray = [];
        for (var i = 0; i < currData.length; i++) {
            var dataArray = []
            for (var z = 0; z < currData[i].length; z++) {
                dataArray.push(currData[i][z]);
            }
            currDataArray.push(dataArray);
        }
        $.ajax({
            type: "POST",
            url: "/NGKBusi/FA/LaborCost/SetWorkingDay",
            dataType: 'json',
            contentType: 'application/json',
            data: JSON.stringify({
                'dataList': currDataArray,
                'iLCWorkingDayPeriod':period
            }),
            tryCount: 0,
            tryLimit: 3,
            success: function (data) {
                alert("Data Saved Successfully!");
                $("#divWorkingDay").LoadingOverlay("hide", true);
            },
            error: function (textStatus) {
                if (textStatus == "timeout") {
                    this.tryCount++;
                    if (this.tryCount <= this.tryLimit) {
                        $.ajax(this);
                        return;
                    }
                }
                $("#divWorkingDay").LoadingOverlay("hide", true);
                alert("Error Occurred, Please Try Again.");
            }
        });
    });
});

function monthDiff(d1, d2) {
    var months;
    months = (d2.getFullYear() - d1.getFullYear()) * 12;
    months -= d1.getMonth() + 1;
    months += d2.getMonth();
    return months <= 0 ? 0 : months;
}