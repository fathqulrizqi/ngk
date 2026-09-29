$(document).ready(function () {
    initData();
    $('#btnLCMasterDataSave').click(function () {
        $("#divMasterData").LoadingOverlay("show");
        var currData = $('#divMasterData').jexcel('getData');
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
            url: "/NGKBusi/FA/LaborCost/SetMasterData",
            dataType: 'json',
            contentType: 'application/json',
            data: JSON.stringify({
                'dataList': currDataArray
            }),
            tryCount: 0,
            tryLimit: 3,
            success: function (data) {
                alert("Data Saved Successfully!");
                $("#divMasterData").LoadingOverlay("hide", true);
            },
            error: function (textStatus) {
                if (textStatus == "timeout") {
                    this.tryCount++;
                    if (this.tryCount <= this.tryLimit) {
                        $.ajax(this);
                        return;
                    }
                }
                $("#divMasterData").LoadingOverlay("hide", true);
                alert("Error Occurred, Please Try Again.");
            }
        });
    });
    $("#btnLCMasterDataUpload").click(function () {
        $("#flLCUpload").click();
    });
    $("#flLCUpload").change(function () {
        if (window.FormData !== undefined) {
            $("#divMasterData").LoadingOverlay("show");

            var fileUpload = $("#flLCUpload").get(0);
            var files = fileUpload.files;

            // Create FormData object  
            var fileData = new FormData();

            // Looping over all files and add it to FormData object  
            for (var i = 0; i < files.length; i++) {
                fileData.append(files[i].name, files[i]);
            }

            $.ajax({
                url: 'UploadMasterData',
                type: "POST",
                contentType: false, // Not to set any content header  
                processData: false, // Not to process data  
                data: fileData,
                success: function (result) {
                    alert(result);
                    $("#divMasterData").LoadingOverlay("hide", true);
                    initData();
                },
                error: function (err) {
                    alert(err.statusText);
                    $("#divMasterData").LoadingOverlay("hide", true);
                }
            });
        } else {
            alert("FormData is not supported.");
        }
        $(this).val('');
    });
});
function initData() {
    var post = $('#divMasterData').data('position');
    $('#divMasterData').jexcel({
        url: "/NGKBusi/FA/LaborCost/GetMasterData",
        colHeaders: ['Period', 'Position<br />(FOR THE NEW EMPLOYEE)', 'Basic Salary', 'Transportation', 'Medical', 'Daily Transportation', 'Job Allowance'
            , 'Shift', 'Insentive - Kehadiran', 'Insentive - 2S3G', 'Meals Allowance', 'Skill Allowance', 'ATM'],
        colWidths: [80, 300, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100],
        tableOverflow: true,
        tableHeight: $(window).height() - 300,
        columns: [
            { type: 'number' },
            { type: 'text'},
            { type: 'number', mask: '#.##0', options: { reverse: true } }, { type: 'number', mask: '#.##0', options: { reverse: true } }, { type: 'number', mask: '#.##0', options: { reverse: true } }, { type: 'number', mask: '#.##0', options: { reverse: true } }, { type: 'number', mask: '#.##0', options: { reverse: true } }, { type: 'number', mask: '#.##0', options: { reverse: true } }, { type: 'number', mask: '#.##0', options: { reverse: true } }, { type: 'number', mask: '#.##0', options: { reverse: true } }
            , { type: 'number', mask: '#.##0', options: { reverse: true } }, { type: 'number', mask: '#.##0', options: { reverse: true } }, { type: 'number', mask: '#.##0', options: { reverse: true } }
            
        ]
    });
    $('#divMasterData').jexcel('updateSettings', {
        table: function (instance, cell, col, row, val, id) {
            if (row % 2) {
                $(cell).css('background-color', '#edf3ff');
            }
        }
    });

}