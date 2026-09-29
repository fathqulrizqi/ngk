$(document).ready(function () {
    var hot;

    $.ajax({
        type: "POST",
        url: "/NGKBusi/Sales/Distributor/getMasterPartData",
        tryCount: 0,
        tryLimit: 3,
        data: {},
        success: function (data) {
            generateHansontable([[], [], [], [], [], [], [], [], [], []], data, document.querySelector('#divPOPartList'));
        }, error: function (xhr, textStatus, errorThrown) {
            // ... existing error handling ...
        }
    });

    $("#btnSalesPOSave").click(function () {
        var currentHeaderID = $("#txtHeaderID").val();
        // 2. Validate Grid Data exists
        if (!hot) {
            swal("Error", "Grid not initialized", "error");
            return;
        }

        // 3. Get all data from Handsontable (removes the last empty spare row if configured)
        var gridData = hot.getData();

        // Filter out completely empty rows if necessary
        var filteredData = gridData.filter(function (row) {
            return row[2] != null && row[2] !== ""; // Check if Product Name is present
        });

        if (filteredData.length === 0) {
            swal("Warning", "Please add at least one item.", "warning");
            return;
        }

        $.ajax({
            type: "POST",
            // 4. Point to the new Action
            url: "/NGKBusi/Sales/Distributor/SavePO",
            tryCount: 0,
            tryLimit: 3,
            data: {
                // Construct the DTO object
                iData: {
                    HeaderID: currentHeaderID,
                    DistroName: $("#txtCustName").val(),
                    DistroID: $("#txtCustCode").val(),
                    PODate: $("#txtPODate").val(),
                    PONo: $("#txtPONo").val(),
                    SONo: $("#txtSONo").val(),
                    Lines: filteredData // Send the array of arrays
                }
            },
            success: function (response) {
                if (response.success) {
                    // Update the hidden field so the next click counts as an "Update"
                    $("#txtHeaderID").val(response.headerID);

                    // Update the Niterra PO Display if needed
                    $("#lblNiterraPO").text(response.niterraPO);

                    swal("Success!", response.message + "\nPO: " + response.niterraPO, "success");

                    // Note: Do NOT reload the page (location.reload()) 
                    // if you want the user to be able to edit immediately after saving.
                    // If you reload, you must ensure the page reloads with the ID in the URL.
                } else {
                    swal("Error!", response.message, "error");
                }
            },
            error: function (xhr, textStatus, errorThrown) {
                if (textStatus === "timeout") {
                    this.tryCount++;
                    if (this.tryCount <= this.tryLimit) {
                        $.ajax(this);
                        return;
                    }
                }
                console.log(xhr);
                alert("Error Occurred during Save: " + errorThrown);
            }
        });
    });

    function generateHansontable(currData, currAutoComplete, container) {


        hot = new Handsontable(container, {
            nestedHeaders: [
                ['Vehicle Category', 'S/P Type', 'Part Name', 'Part Number', 'Nomor PO Distro', 'Quantity'],
            ],
            themeName: 'ht-theme-main',
            data: currData,
            columns: [{ readOnly: true }, { readOnly: true }, {
                type: 'autocomplete',
                data: "productname",
                width: '135px',
                source: currAutoComplete.Data,
                strict: true
            }, { readOnly: true }, { readOnly: true }, { type: 'numeric', width: '35px', className: 'htRight', numericFormat: { pattern: '0,00', culture: 'en-US' } }],
            colHeaders: true,
            rowHeaders: true,
            contextMenu: ['row_above', 'row_below', 'remove_row', '---------', 'undo', 'redo', '---------', 'copy', 'cut'],
            filters: true,
            dropdownMenu: true,
            stretchH: 'all',
            height: 'auto',
            width: 'auto',
            mergeCells: false,
            manualColumnResize: true,
            autoWrapRow: true,
            autoWrapCol: true,
            minSpareRows: 1,
            licenseKey: 'non-commercial-and-evaluation',
            afterChange: function (changes, source) {
                // 'changes' is an array of arrays, where each inner array contains:
                // [row, prop, oldValue, newValue]      

                if (changes) {
                    changes.forEach(([row, prop, oldValue, newValue]) => {
                        if (prop === 'productname' && oldValue !== newValue) {
                            var _idx = $.inArray(newValue, currAutoComplete.Data);
                            // Safety check in case autocomplete value is invalid
                            if (_idx > -1) {
                                var currCategory = currAutoComplete.Category[_idx];
                                var currSPType = currAutoComplete.SPType[_idx];
                                var currPartNumber = currAutoComplete.PartNumber[_idx];
                                var currDistroPONo = $("#txtPONo").val();

                                // Use 'hot' instead of 'this' to be safe, or keep 'this' if context is correct
                                hot.setDataAtRowProp(row, 0, currCategory);
                                hot.setDataAtRowProp(row, 1, currSPType);
                                hot.setDataAtRowProp(row, 3, currPartNumber);
                                hot.setDataAtRowProp(row, 4, currDistroPONo);
                            }
                        }
                    });
                }
            }
        });
        $("#txtPONo").change(function () {
            for (var i = 0; i < hot.countRows(); i++) { // Changed <= to <
                if (hot.getDataAtCell(i, 2) != null) {
                    hot.setDataAtRowProp(i, 4, $(this).val());
                }
            }
        });
    }
});