$(document).ready(function () {

    var period = parseInt($(".selLCEmployeeDataPeriod").val(), 10);
    initData(period);
    $('#btnLCEmployeeDataSave').click(function () {
        $("#divEmployeeData").LoadingOverlay("show");
        var currData = $('#divEmployeeData').jexcel('getData');
        var currDataArray = [];
        for (var i = 0; i < currData.length; i++) {
            var dataArray = [];
            for (var z = 0; z < currData[i].length; z++) {
                dataArray.push(currData[i][z]);
            }
            currDataArray.push(dataArray);
        }
        $.ajax({
            type: "POST",
            url: "/NGKBusi/FA/LaborCost/SetEmployeeData",
            dataType: 'json',
            contentType: 'application/json',
            data: JSON.stringify({
                'dataList': currDataArray,
                'iLCEmployeeDataPeriod': period
            }),
            tryCount: 0,
            tryLimit: 3,
            success: function (data) {
                alert("Data Saved Successfully!");
                $("#divEmployeeData").LoadingOverlay("hide", true);
            },
            error: function (textStatus) {
                if (textStatus === "timeout") {
                    this.tryCount++;
                    if (this.tryCount <= this.tryLimit) {
                        $.ajax(this);
                        return;
                    }
                }
                $("#divEmployeeData").LoadingOverlay("hide", true);
                alert("Error Occurred, Please Try Again.");
            }
        });
    });
    $("#btnLCEmployeeDataUpload").click(function () {
        $("#flLCUpload").click();
    });
    $("#selLCEmployeeDataPeriod").change(function () {
        initData(parseInt($(".selLCEmployeeDataPeriod").val(), 10));
    });
    $("#flLCUpload").change(function () {
        if (window.FormData !== undefined) {
            $("#divEmployeeData").LoadingOverlay("show");

            var fileUpload = $("#flLCUpload").get(0);
            var files = fileUpload.files;

            // Create FormData object  
            var fileData = new FormData();

            // Looping over all files and add it to FormData object  
            for (var i = 0; i < files.length; i++) {
                fileData.append(files[i].name, files[i]);
            }
            fileData.append('iPeriod', period);

            $.ajax({
                url: 'UploadEmployeeData',
                type: "POST",
                contentType: false, // Not to set any content header  
                processData: false, // Not to process data  
                data: fileData,
                success: function (result) {
                    alert(result);
                    $("#divEmployeeData").LoadingOverlay("hide", true);
                    initData(period);
                },
                error: function (err) {
                    alert(err.statusText);
                    $("#divEmployeeData").LoadingOverlay("hide",true);
                }
            });
        } else {
            alert("FormData is not supported.");
        }  
        $(this).val('');
    });
});

function initData(period) {
    $('#divEmployeeData').jexcel({
        url: "/NGKBusi/FA/LaborCost/GetEmployeeData?iLCEmployeeDataPeriod=" + period,
        colHeaders: ['NIK', 'Name', 'Basic Salary', 'Medical', 'Fix Transportation', 'Total Fix Income', 'Daily Transportation', 'Job Allowance', 'Meal'
            , 'Incentive', 'Incentive (2S3G)', 'Overtime', 'Shift', 'THR/TAT', 'Rapel', 'Others', 'ATM', 'Skill Allow', 'PPH 21', 'Total Unfix Income', 'Gross Income'],
        colWidths: [80, 300, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100],
        tableOverflow: true,
        tableHeight: $(window).height() - 300,
        columns: [
            { type: 'text' },
            { type: 'text' },
            { type: 'number', mask: '#.##0', options: { reverse: true } }, { type: 'number', mask: '#.##0', options: { reverse: true } }, { type: 'number', mask: '#.##0', options: { reverse: true } }, { type: 'number', mask: '#.##0', options: { reverse: true } }, { type: 'number', mask: '#.##0', options: { reverse: true } }, { type: 'number', mask: '#.##0', options: { reverse: true } }, { type: 'number', mask: '#.##0', options: { reverse: true } }, { type: 'number', mask: '#.##0', options: { reverse: true } }
            , { type: 'number', mask: '#.##0', options: { reverse: true } }, { type: 'number', mask: '#.##0', options: { reverse: true } }, { type: 'number', mask: '#.##0', options: { reverse: true } }, { type: 'number', mask: '#.##0', options: { reverse: true } }, { type: 'number', mask: '#.##0', options: { reverse: true } }, { type: 'number', mask: '#.##0', options: { reverse: true } }, { type: 'number', mask: '#.##0', options: { reverse: true } }, { type: 'number', mask: '#.##0', options: { reverse: true } }
            , { type: 'number', mask: '#.##0', options: { reverse: true } }, { type: 'number', mask: '#.##0', options: { reverse: true } }, { type: 'number', mask: '#.##0', options: { reverse: true } }
        ]
    });
    $('#divEmployeeData').jexcel('updateSettings', {
        table: function (instance, cell, col, row, val, id) {
            // Odd row colours
            if (row % 2) {
                $(cell).css('background-color', '#edf3ff');
            }
        }
    });
}