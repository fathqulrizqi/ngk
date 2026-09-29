$(document).ready(function () {
    initData();
    $('#btnLCIncrementFactorSave').click(function () {
        $("#divIncrementFactorData").LoadingOverlay("show");
        var currData = $('#divIncrementFactorData').jexcel('getData');
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
            url: "/NGKBusi/FA/LaborCost/SetIncrementFactorData",
            dataType: 'json',
            contentType: 'application/json',
            data: JSON.stringify({
                'dataList': currDataArray
            }),
            tryCount: 0,
            tryLimit: 3,
            success: function (data) {
                alert("Data Saved Successfully!");
                $("#divIncrementFactorData").LoadingOverlay("hide", true);
            },
            error: function (textStatus) {
                if (textStatus == "timeout") {
                    this.tryCount++;
                    if (this.tryCount <= this.tryLimit) {
                        $.ajax(this);
                        return;
                    }
                }
                $("#divIncrementFactorData").LoadingOverlay("hide", true);
                alert("Error Occurred, Please Try Again.");
            }
        });
    });
    $("#btnLCIncrementFactorUpload").click(function () {
        $("#flLCUpload").click();
    });
    $("#flLCUpload").change(function () {
        if (window.FormData !== undefined) {
            $("#divIncrementFactorData").LoadingOverlay("show");

            var fileUpload = $("#flLCUpload").get(0);
            var files = fileUpload.files;

            // Create FormData object  
            var fileData = new FormData();

            // Looping over all files and add it to FormData object  
            for (var i = 0; i < files.length; i++) {
                fileData.append(files[i].name, files[i]);
            }

            $.ajax({
                url: 'UploadIncrementFactorData',
                type: "POST",
                contentType: false, // Not to set any content header  
                processData: false, // Not to process data  
                data: fileData,
                success: function (result) {
                    alert(result);
                    $("#divIncrementFactorData").LoadingOverlay("hide", true);
                    initData();
                },
                error: function (err) {
                    alert(err.statusText);
                    $("#divIncrementFactorData").LoadingOverlay("hide", true);
                }
            });
        } else {
            alert("FormData is not supported.");
        }
        $(this).val('');
    });
});
function initData() {
    var post = $('#divIncrementFactorData').data('position');
    $('#divIncrementFactorData').jexcel({
        url: "/NGKBusi/FA/LaborCost/getIncrementFactorData",
        colHeaders: ['Period', 'Position', 'Basic Salary', 'Transportation', 'Medical', 'Daily Transportation', 'Job Allowance'
            , 'Shift', 'Insentive - Kehadiran', 'Insentive - 2S3G', 'Meals Allowance', 'Skill Allowance', 'ATM'],
        colWidths: [80, 300, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100],
        tableOverflow: true,
        tableHeight: $(window).height() - 300,
        columns: [
            { type: 'number' },
            { type: 'dropdown', source: post},
            { type: 'text' }, { type: 'text' }, { type: 'text' }, { type: 'text' }, { type: 'text' }, { type: 'text' }, { type: 'text' }, { type: 'text' }
            , { type: 'text' }, { type: 'text' }, { type: 'text' }
            
        ]
    });
    $('#divIncrementFactorData').jexcel('updateSettings', {
        table: function (instance, cell, col, row, val, id) {
            if (row % 2) {
                $(cell).css('background-color', '#edf3ff');
            }
        }
    });

}