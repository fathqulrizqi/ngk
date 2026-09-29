$(document).ready(function () {
    $.ajax({
        type: "POST",
        url: "/NGKBusi/Sales/Distributor/getMasterPart",
        tryCount: 0,
        tryLimit: 3,
        data: {},
        success: function (data) {
            generateHansontable(data, document.querySelector('#divMasterPartList'));
        }, error: function (xhr, textStatus, errorThrown) {
            if (textStatus === "timeout") {
                this.tryCount++;
                if (this.tryCount <= this.tryLimit) {
                    $.ajax(this);
                    return;
                }
            }
            console.log(xhr)
            console.log(textStatus)
            console.log(errorThrown)
            alert("Error Occurred, Please try again !");
        }
    });
    function generateHansontable(currData, container) {


        const hot = new Handsontable(container, {
            nestedHeaders: [
                ['Product Name', 'VehicleID', 'S/P Type', 'Item ID', 'PM Category QTR', 'Price'],
            ],
            data: currData,
            columns: [{}, {}, {}, {}, {}, { type: 'numeric', width: '35px', className: 'htRight', numericFormat: { pattern: '0,00', culture: 'en-US' } }],
            colHeaders: true,
            filters: true,
            dropdownMenu: true,
            filters: true,
            stretchH: 'all',
            height: 'auto',
            width: 'auto',
            mergeCells: false,
            manualColumnResize: true,
            autoWrapRow: true,
            autoWrapCol: true,
            minSpareRows: 1,
            licenseKey: 'non-commercial-and-evaluation'
        });

        $(".btnMasterSaveData").click(function () {
            hot.validateCells((valid) => {
                if (valid) {
                    var _currData = hot.getSourceDataArray();
                    console.log(_currData)
                    $.ajax({
                        type: "POST",
                        url: "/NGKBusi/Sales/Distributor/setMasterPart",
                        tryCount: 0,
                        tryLimit: 3,
                        data: {
                            iData: _currData.slice(0, -1)
                        },
                        success: function (data) {
                            swal("Success!", "Data has been saved!", "success");
                        }, error: function (xhr, textStatus, errorThrown) {
                            if (textStatus === "timeout") {
                                this.tryCount++;
                                if (this.tryCount <= this.tryLimit) {
                                    $.ajax(this);
                                    return;
                                }
                            }
                            console.log(xhr)
                            console.log(textStatus)
                            console.log(errorThrown)
                            alert("Error Occurred, Please try again !");
                        }
                    });
                } else {
                    swal("Invalid!", "Please check your data format!", "error");
                }
            })
        });
    }
});