$(document).ready(function () {



    var isFormDirty = false;
    var $unsavedWarning = $('#unsavedWarning');

    function setFormDirty() {
        isFormDirty = true;
        $unsavedWarning.show();
    }
    function setFormClean() {
        isFormDirty = false;
        $unsavedWarning.hide();
    }

    // Mark form as dirty on any change in form fields inside divMedicalClaimForm and modalPatient
    $('.divMedicalClaimForm,.modalPatient').on('input change', 'input, select, textarea', function (e) {
        setFormDirty();
    });

    new AutoNumeric.multiple(".txtNumeric", { unformatOnSubmit: true, allowDecimalPadding: false });

    var tbMedicalClaim, tbClaimLine, tbClaimDetail, tbMedicalDataList;
    // Added variable to track the current claim ID (0 for new, >0 for existing)
    var activeClaimId = 0;
    var claimLinesArrayOrder = [];
    var claimLinesArraySelected = 0;
    var subCategoryData = [{
        category: "Inpatient", subCategory: ["BPJS Health COB", "BPJS Health COB 75%", "Full Cover Company"]
    }, {
        category: "Outpatient", subCategory: ["Employee", "Family"]
    }, {
        category: "Dental Care", subCategory: ["Employee", "Family 50%"]
    }, {
        category: "Glasses Claim", subCategory: ["Frame + Lens", "Lens"]
    }, {
        category: "Obituary", subCategory: ["Parents/In-Laws", "Husband/Wife", "Child"]
    }];
    var defaultDeleteRowButton = '<button type="button" class="btn btn-sm btn-danger inline-button btnDeleteRow">Delete</button>';
    var defaultEditRowButton = '<button type="button" class="btn btn-sm btn-warning inline-button btnEditRow mr-2">Edit</button>';
    function defaultEmployeeTextbox(value) {
        return '<input type="text" name="iNIK[]" class="form-control txtClaimNIK" placeholder="Employee" value="' + value + '" /><label class="d-none">' + value + '</label>';
    }
    var defaultLinesData = { employee: defaultEmployeeTextbox(""), division: "-", department: "-", position: "-", costname: "-", totalDeduction: "0", details: [], deletebutton: defaultDeleteRowButton };

    var claimLinesData = [defaultLinesData];
    var claimDetailsData = [];

    function updateLineDeduction(lineIndex) {
        var total = 0;
        if (claimLinesData[lineIndex] && claimLinesData[lineIndex].details) {
            claimLinesData[lineIndex].details.forEach(function (d) {
                var val = parseFloat(d.Deduction) || 0;
                total += val;
            });
        }

        // Format and update data object
        claimLinesData[lineIndex].totalDeduction = AutoNumeric.format(total, { allowDecimalPadding: false });

        // Update the specific row in DataTable if it exists
        if (tbClaimLine) {
            // We use the lineIndex to target the specific DataTables row
            tbClaimLine.row(lineIndex).data(claimLinesData[lineIndex]).draw(false);
        }
    }

    function calculateGrandTotals() {
        var totalInvoice = 0;
        var totalActual = 0;
        var totalSaving = 0;

        // Iterate through all Employee Lines
        if (claimLinesData && claimLinesData.length > 0) {
            claimLinesData.forEach(function (line) {
                // Iterate through all Details (Patients) for this line
                if (line.details && Array.isArray(line.details)) {
                    line.details.forEach(function (det) {
                        // Parse values (handle strings or numbers)
                        var inv = parseFloat(det.InvoiceAmount) || 0;
                        var act = parseFloat(det.ActualAmount) || 0;
                        var sav = parseFloat(det.Saving) || 0;

                        totalInvoice += inv;
                        totalActual += act;
                        totalSaving += sav;
                    });
                }
            });
        }

        // Update UI with formatted numbers
        $('#lblTotalInvoice').text(AutoNumeric.format(totalInvoice, { allowDecimalPadding: false }));
        $('#lblTotalActual').text(AutoNumeric.format(totalActual, { allowDecimalPadding: false }));
        $('#lblTotalSaving').text(AutoNumeric.format(totalSaving, { allowDecimalPadding: false }));
    }

    // medical claims list table
    function loadMedicalList() {
        var selectedYear = $('#selFilterYear').val(); // Get value
        $.ajax({
            url: '/NGKbusi/HC/Medical/GetClaims',
            type: 'GET',
            data: { year: selectedYear },
            dataType: 'json',
            success: function (data) {
                if (!tbMedicalDataList) {
                    tbMedicalDataList = $('#tblMedicalDataList').DataTable({
                        data: data,
                        columns: [
                            { data: 'invoice' },
                            { data: 'thirdPartyName' },
                            { data: 'createdAtText' },
                            { data: 'linesCount' },
                            {
                                // New Column for Action
                                data: null,
                                orderable: false,
                                className: "text-center",
                                render: function (data, type, row) {
                                    return '<button type="button" class="btn btn-danger btn-sm btnDeleteClaim" data-id="' + row.id + '">Delete</button>';
                                }
                            }
                        ],
                        paging: true,
                        destroy: true,
                        ordering: { indicators: false },
                        columnControl: [
                            {
                                target: 0,
                                content: ['order', ['search', 'searchList']]
                            }
                        ]

                    });

                    // Row click to load data (existing)
                    $('#tblMedicalDataList tbody').on('click', 'tr', function (e) {
                        // If the clicked element is the delete button, do nothing here (handled below)
                        if ($(e.target).hasClass('btnDeleteClaim')) return;

                        var row = tbMedicalDataList.row(this).data();
                        if (!row) return;
                        loadClaimIntoForm(row.id);
                        $("#btnSubmitClaim").text('Save Claim');
                    });

                    // Delete button click handler
                    $('#tblMedicalDataList tbody').on('click', '.btnDeleteClaim', function (e) {
                        e.stopPropagation(); // Stop the row click event
                        var id = $(this).data('id');

                        if (!confirm('Are you sure you want to delete this claim? This cannot be undone.')) return;

                        $.ajax({
                            url: '/NGKbusi/HC/Medical/DeleteClaim',
                            type: 'POST',
                            data: { id: id },
                            success: function (res) {
                                if (res.success) {
                                    alert('Claim deleted successfully.');
                                    // If we deleted the currently loaded claim, reset the form (optional but good UX)
                                    if (activeClaimId == id) {
                                        location.reload(); // Simple way to clear everything
                                    } else {
                                        loadMedicalList();
                                    }
                                } else {
                                    alert('Failed to delete: ' + res.message);
                                }
                            },
                            error: function () {
                                alert('Error deleting claim.');
                            }
                        });
                    });

                } else {
                    tbMedicalDataList.clear().rows.add(data).draw();
                }
            },
            error: function () {
                console.error('Failed loading claims list');
            }
        });
    }
    // 3. Event Listener for Filter Change
    $('#selFilterYear').on('change', function () {
        loadMedicalList();
    });

    $("#btnNewClaim").on('click', function () {
        $(".divMedicalClaimForm").LoadingOverlay("show");
        $("#formMedicalClaim")[0].reset();
        $("#selThirdParty").val(null).trigger('change');
        $(".divMedicalClaimForm").fadeIn();
        activeClaimId = 0;
        claimLinesData = [defaultLinesData];
        initClaimLinesTable([], claimLinesData);
        calculateGrandTotals();
        $('.fieldMedicalDetails').hide();
        $(".divMedicalClaimForm").LoadingOverlay("hide");
        $("#btnSubmitClaim").text('Submit Claim');
        setFormClean();
    });

    function loadClaimIntoForm(id) {
        // Set the active claim ID when loading
        $(".divMedicalClaimForm").LoadingOverlay("show");
        activeClaimId = id;

        $.ajax({
            url: '/NGKbusi/HC/Medical/GetClaim',
            type: 'GET',
            data: { id: id },
            dataType: 'json',
            success: function (res) {
                if (!res || !res.success) {
                    alert('Unable to load claim');
                    return;
                }

                var claim = res.claim;
                // populate header
                $('#txtInvoice').val(claim.invoice);
                $('#selThirdParty').val(claim.thirdParty).trigger('change');
                $('#txtHospitalClinic').val(claim.hospitalClinic);

                // build claimLinesData from claim.lines
                claimLinesData = [];
                if (Array.isArray(claim.lines) && claim.lines.length > 0) {
                    claim.lines.forEach(function (l) {
                        var sumDeduction = 0; // Init Sum
                        var ln = {
                            employee: defaultEmployeeTextbox(l.employee || ""),
                            division: l.division || "-",
                            department: l.department || "-",
                            position: l.position || "-",
                            costname: l.costname || "-",
                            details: [],
                            deletebutton: defaultDeleteRowButton
                        };

                        if (Array.isArray(l.details)) {
                            l.details.forEach(function (d) {
                                // Calculate sum while looping details
                                sumDeduction += (parseFloat(d.Deduction) || 0);
                                ln.details.push({
                                    PatientName: d.PatientName || "-",
                                    Beneficiary: d.Beneficiary || "-",
                                    TreatmentCategory: d.TreatmentCategory || "-",
                                    SubCategory: d.SubCategory || "-",
                                    Diagnosa: d.Diagnosa || "-",
                                    StartEndDate: d.StartEndDate || "-",
                                    InvoiceAmount: d.InvoiceAmount || "0",
                                    ActualAmount: d.ActualAmount || "0",
                                    Saving: d.Saving || "0",
                                    Deduction: d.Deduction || "0",
                                    actionButton: defaultEditRowButton + defaultDeleteRowButton
                                });
                            });
                        }
                        ln.totalDeduction = AutoNumeric.format(sumDeduction, { allowDecimalPadding: false });
                        claimLinesData.push(ln);
                    });
                } else {
                    claimLinesData = [defaultLinesData];
                }

                // re-init claim lines table and details
                var _order = tbClaimLine ? tbClaimLine.order() : [];
                initClaimLinesTable(_order, claimLinesData);

                // select first line (if any) and show details
                claimLinesArraySelected = 0;
                calculateGrandTotals();
                //initClaimDetailsTable([], claimLinesData[0] ? claimLinesData[0].details : []);
                $(".divMedicalClaimForm").fadeIn();
                //$('.fieldMedicalDetails').show();
                $('.fieldMedicalDetails').hide();
                $(".divMedicalClaimForm").LoadingOverlay("hide");
                window.scrollTo({ top: 0, behavior: 'smooth' });
                setFormClean();
            },
            error: function () {
                alert('Error loading claim data');
                $(".divMedicalClaimForm").LoadingOverlay("hide");
            }
        });
    }

    function initClaimLinesTable($order = [], $data) {
        if ($.fn.DataTable.isDataTable('.tblClaimLines')) {
            tbClaimLine.clear().destroy();
        }
        var linesData = $data != null ? $data : (claimLinesData.length > 0 ? claimLinesData : [defaultLinesData]);
        tbClaimLine = $('.tblClaimLines').DataTable({
            paging: false,
            destroy: true,
            ordering: { indicators: false },
            order: $order,
            columns: [
                { data: 'employee', name: "Employee" },
                { data: 'division', name: "Division" },
                { data: 'department', name: "Department" },
                { data: 'position', name: "Position" },
                { data: 'costname', name: "CostName" },
                { data: 'totalDeduction', name: "TotalDeduction", className: "text-right" },
                { data: 'deletebutton', className: "text-center" }
            ],
            data: linesData,
            columnDefs: [{
                targets: [0],
                columnControl: [
                    {
                        target: 0,
                        content: ['order', ['search']]
                    }
                ],
            }, {
                targets: [1, 2, 3, 4, 5],
                columnControl: [
                    {
                        target: 0,
                        content: ['order', ['search', 'searchList']]
                    }
                ],
            }]

        });

        $('table.tblClaimLines tbody tr').off('click').on('click', function () {
            claimLinesArrayOrder = tbClaimLine.rows({ order: 'applied' }).indexes().toArray();
            claimLinesArraySelected = claimLinesArrayOrder[$(this).closest('tr').index()] || 0;
            if (claimLinesData[claimLinesArraySelected].division != "-") {
                $(".fieldMedicalDetails .spanLegend").text(claimLinesData[claimLinesArraySelected] && claimLinesData[claimLinesArraySelected].division != "-" ? " | " + $(this).closest('tr').find("label").text() : "");
                $(".fieldMedicalDetails").hide().fadeIn();
                initClaimDetailsTable(tbClaimDetail ? tbClaimDetail.order() : [], claimLinesData[claimLinesArraySelected].details || []);
            }
        });
    }
    initClaimLinesTable();

    $("#selThirdParty").change(function () {
        var currThirdParty = $(this).val().split("|");
        if (currThirdParty.length > 1 && currThirdParty[0].substring(0, 1) === "E") 
        {
            $("#txtHospitalClinic").val(currThirdParty[1]);
        }
    });

    // autocomplete and other handlers (unchanged)
    $('.tblClaimLines').on('focus', '.txtClaimNIK', function () {
        var currCTR = $(this);
        if (!$(this).data('ui-autocomplete')) {
            $(this).autocomplete({
                source: function (request, response) {
                    $.ajax({
                        url: '/NGKbusi/HC/Medical/GetNIKSuggestions',
                        type: 'GET',
                        dataType: 'json',
                        data: { term: request.term },
                        success: function (data) {
                            var excludeValues = [];
                            $(".txtClaimNIK").each(function () {
                                var val = $(this).val().toLowerCase();
                                if (val && val !== request.term.toLowerCase()) { excludeValues.push(val); }
                            });
                            var filteredData = data.filter(function (item) {
                                return $.inArray(item.value.toLowerCase(), excludeValues) == -1;
                            });
                            response(filteredData);
                        }
                    });
                },
                select: function (event, ui) {
                    var index = currCTR.closest('tr').index();
                    var newData = {
                        employee: defaultEmployeeTextbox(ui.item.value),
                        division: ui.item.division || "-",
                        department: ui.item.department || "-",
                        position: ui.item.position || "-",
                        costname: ui.item.costname || "-",
                        totalDeduction: "0", // Ensure this property is present
                        details: [],
                        deletebutton: defaultDeleteRowButton
                    };
                    if (claimLinesData[index]) { claimLinesData[index] = newData; } else { claimLinesData.push(newData); }
                    var _order = tbClaimLine.order();
                    initClaimLinesTable(_order);
                }
            });
        }
    });

    // Add/delete rows handlers (unchanged)
    $('table.tblClaimLines').on('click', '#btnAddRow', function () {
        claimLinesData.push(defaultLinesData);
        initClaimLinesTable(tbClaimLine.order(), claimLinesData);
    });

    $('table.tblClaimLines').on('click', '.btnDeleteRow', function () {
        if (confirm('Are you sure want to delete this data ?')) {
            claimLinesArrayOrder = tbClaimLine.rows({ order: 'applied' }).indexes().toArray();
            var $rows = $('table.tblClaimLines tbody tr');
            if ($rows.length > 1) {
                tbClaimLine.row($(this).closest('tr')).remove();
                claimLinesData.splice(claimLinesArrayOrder[$(this).closest('tr').index()], 1);
            } else {
                $(this).closest('tr').find('input').val('');
                claimLinesData = [defaultLinesData];
                claimLinesData[0].details = [];
            }
            initClaimLinesTable(tbClaimLine.order(), claimLinesData);
            calculateGrandTotals();
        }
    });

    function initClaimDetailsTable($order = [], $data) {
        if ($.fn.DataTable.isDataTable('.tblClaimDetails')) {
            tbClaimDetail.clear().destroy();
        }
        var detailsData = $data != null ? $data : (claimDetailsData.length > 0 ? claimDetailsData : []);
        tbClaimDetail = $('.tblClaimDetails').DataTable({
            paging: false,
            destroy: true,
            ordering: { indicators: false },
            order: $order,
            columns: [
                { data: 'PatientName' },
                { data: 'Beneficiary' },
                { data: 'TreatmentCategory' },
                { data: 'SubCategory' },
                { data: 'Diagnosa' },
                { data: 'StartEndDate' },
                { data: 'InvoiceAmount', className: 'tdNumber' },
                { data: 'ActualAmount', className: 'tdNumber' },
                { data: 'Saving', className: 'tdNumber' },
                { data: 'Deduction', className: 'tdNumber' },
                { data: 'actionButton', className: 'text-nowrap text-center' }],
            data: detailsData,
            columnDefs: [{
                targets: [0, 4, 5, 6, 7, 8, 9],
                columnControl: [
                    {
                        target: 0,
                        content: ['order', ['search']]
                    }
                ],
            }, {
                targets: [1, 2, 3],
                columnControl: [
                    {
                        target: 0,
                        content: ['order', ['search', 'searchList']]
                    }
                ],
            }], initComplete: function (settings, json) {
                $(".tdNumber").each(function () {
                    if ($(this).text() === "" || isNaN($(this).text())) return;

                    $(this).text(AutoNumeric.format($(this).text(), { allowDecimalPadding: false }));
                });
            }
        });
    }
    initClaimDetailsTable();

    // patient modal / details add/edit/delete (kept from prior implementation)
    $(document).on("click", "#btnAddPatient", function () {
        if (typeof claimLinesArraySelected === 'undefined' || claimLinesData.length === 0) {
            alert("Please select an Employee line first.");
            return;
        }
        $('#iDetailId').val('');
        $('#iLineIndex').val(claimLinesArraySelected);
        $('#iPatientName,#txtDiagnosa,#txtStartDate,#txtEndDate,#txtInvoiceAmount,#txtActualAmount,#txtSaving,#txtDeduction').val('');
        $('#selBeneficiary,#selTreatmentCategory,#selSubCategory').val('').trigger('change');
        $('.modalPatient').modal('show');
    });

    $(document).on('input', '#txtInvoiceAmount, #txtActualAmount', function () {
        var inv = parseFloat(AutoNumeric.unformat($('#txtInvoiceAmount').val())) || 0;
        var act = parseFloat(AutoNumeric.unformat($('#txtActualAmount').val())) || 0;
        $('#txtSaving').val(AutoNumeric.format((inv - act), { allowDecimalPadding: false }));
    });

    $(document).on('click', '.modalPatient .btn-primary', function (e) {
        e.preventDefault();
        var lineIdx = parseInt($('#iLineIndex').val());
        if (isNaN(lineIdx) || !claimLinesData[lineIdx]) {
            alert("Invalid line selected. Please select an employee line first.");
            return;
        }
        var detailId = $('#iDetailId').val();
        var patientName = $('#iPatientName').val() || "-";
        var beneficiary = $('#selBeneficiary').val() || "-";
        var treatmentCategory = $('#selTreatmentCategory').val() || "-";
        var subCategory = $('#selSubCategory').val() || "-";
        var diagnosa = $('#txtDiagnosa').val() || "-";
        var startDate = $('#txtStartDate').val();
        var endDate = $('#txtEndDate').val();
        var startEnd = "-";
        if (startDate && endDate) startEnd = startDate + " - " + endDate;
        else if (startDate) startEnd = startDate;
        else if (endDate) startEnd = endDate;

        var invoiceAmount = $('#txtInvoiceAmount').val() !== "" ? AutoNumeric.unformat($('#txtInvoiceAmount').val()) : 0;
        var actualAmount = $('#txtActualAmount').val() !== "" ? AutoNumeric.unformat($('#txtActualAmount').val()) : 0;
        var saving = invoiceAmount - actualAmount;
        var deduction = $('#txtDeduction').val() !== "" ? AutoNumeric.unformat($('#txtDeduction').val()) : 0;

        var detailObj = {
            PatientName: patientName,
            Beneficiary: beneficiary,
            TreatmentCategory: treatmentCategory,
            SubCategory: subCategory,
            Diagnosa: diagnosa,
            StartEndDate: startEnd,
            InvoiceAmount: invoiceAmount,
            ActualAmount: actualAmount,
            Saving: saving,
            Deduction: deduction,
            actionButton: defaultEditRowButton + defaultDeleteRowButton
        };

        if (!Array.isArray(claimLinesData[lineIdx].details)) { claimLinesData[lineIdx].details = []; }

        if (detailId !== '') {
            var idx = parseInt(detailId);
            if (!isNaN(idx) && claimLinesData[lineIdx].details[idx]) {
                claimLinesData[lineIdx].details[idx] = detailObj;
            } else {
                claimLinesData[lineIdx].details.push(detailObj);
            }
        } else {
            claimLinesData[lineIdx].details.push(detailObj);
        }
        initClaimDetailsTable(tbClaimDetail ? tbClaimDetail.order() : [], claimLinesData[lineIdx].details);
        updateLineDeduction(lineIdx);
        calculateGrandTotals();
        $('.modalPatient').modal('hide');
    });

    $('table.tblClaimDetails').on('click', '.btnEditRow', function () {
        if (!tbClaimDetail) return;
        var detailsOrder = tbClaimDetail.rows({ order: 'applied' }).indexes().toArray();
        var displayIndex = $(this).closest('tr').index();
        var sourceIndex = detailsOrder[displayIndex];
        var lineIdx = claimLinesArraySelected;
        if (!claimLinesData[lineIdx] || !claimLinesData[lineIdx].details || claimLinesData[lineIdx].details.length === 0) return;
        var detail = claimLinesData[lineIdx].details[sourceIndex];
        if (!detail) return;

        $('#iDetailId').val(sourceIndex);
        $('#iLineIndex').val(lineIdx);
        $('#iPatientName').val(detail.PatientName && detail.PatientName !== "-" ? detail.PatientName : "");
        $('#selBeneficiary').val(detail.Beneficiary && detail.Beneficiary !== "-" ? detail.Beneficiary : "").trigger('change');
        $('#selTreatmentCategory').val(detail.TreatmentCategory && detail.TreatmentCategory !== "-" ? detail.TreatmentCategory : "").trigger('change');
        var subCategories = subCategoryData.find(item => item.category === (detail.TreatmentCategory || ''))?.subCategory || [];
        var $subCategorySelect = $("#selSubCategory");
        $subCategorySelect.find("option[value!='']").remove();
        $.each(subCategories, function (index, value) { $subCategorySelect.append($('<option>', { value: value, text: value })); });
        $('#selSubCategory').val(detail.SubCategory && detail.SubCategory !== "-" ? detail.SubCategory : "").trigger('change');
        $('#txtDiagnosa').val(detail.Diagnosa && detail.Diagnosa !== "-" ? detail.Diagnosa : "");
        if (detail.StartEndDate && detail.StartEndDate !== "-") {
            var parts = detail.StartEndDate.split(' - ');
            $('#txtStartDate').val(parts[0] || '').datepicker("option", "maxDate", parts[1] || '');
            $('#txtEndDate').val(parts[1] || '').datepicker("option", "minDate", parts[0] || '');
        } else {
            $('#txtStartDate').val('');
            $('#txtEndDate').val('');
        }
        AutoNumeric.getAutoNumericElement('#txtInvoiceAmount').set(detail.InvoiceAmount && detail.InvoiceAmount !== "-" ? AutoNumeric.format(detail.InvoiceAmount, { allowDecimalPadding: false }) : '');
        AutoNumeric.getAutoNumericElement('#txtActualAmount').set(detail.ActualAmount && detail.ActualAmount !== "-" ? AutoNumeric.format(detail.ActualAmount, { allowDecimalPadding: false }) : '');
        AutoNumeric.getAutoNumericElement('#txtSaving').set(detail.Saving && detail.Saving !== "-" ? AutoNumeric.format(detail.Saving, { allowDecimalPadding: false }) : '');
        AutoNumeric.getAutoNumericElement('#txtDeduction').set(detail.Deduction && detail.Deduction !== "-" ? AutoNumeric.format(detail.Deduction, { allowDecimalPadding: false }) : '');

        $('.modalPatient').modal('show');
    });

    $('table.tblClaimDetails').on('click', '.btnDeleteRow', function () {
        if (!tbClaimDetail) return;
        if (!confirm('Are you sure want to delete this data ?')) return;
        var detailsOrder = tbClaimDetail.rows({ order: 'applied' }).indexes().toArray();
        var displayIndex = $(this).closest('tr').index();
        var sourceIndex = detailsOrder[displayIndex];
        var lineIdx = claimLinesArraySelected;
        if (!claimLinesData[lineIdx] || !claimLinesData[lineIdx].details || !claimLinesData[lineIdx].details[sourceIndex]) return;
        claimLinesData[lineIdx].details.splice(sourceIndex, 1);
        initClaimDetailsTable(tbClaimDetail ? tbClaimDetail.order() : [], claimLinesData[lineIdx].details);
        updateLineDeduction(lineIdx);
        calculateGrandTotals();
    });

    // Submit claim AJAX (updated with ID)
    $('#btnSubmitClaim').on('click', function (e) {
        e.preventDefault();

        var invoice = $('#txtInvoice').val();
        var thirdParty = $('#selThirdParty').val();
        var hospitalClinic = $('#txtHospitalClinic').val();

        if (!invoice || !thirdParty) {
            alert('Invoice and Thirdparty are required.');
            return;
        }

        // Only include id for update
        var payload = {
            invoice: invoice,
            thirdParty: thirdParty,
            hospitalClinic: hospitalClinic,
            lines: []
        };
        if (activeClaimId > 0) {
            payload.id = activeClaimId;
        }

        for (var i = 0; i < claimLinesData.length; i++) {
            var line = claimLinesData[i];
            var empVal = "";
            try {
                var $tmp = $(line.employee);
                empVal = $tmp.filter('input').val() || $tmp.find('input').val() || $tmp.filter('label').text() || "";
            } catch (err) {
                var m = (line.employee || "").match(/value="([^"]*)"/);
                empVal = m ? m[1] : "";
            }

            var details = [];
            if (Array.isArray(line.details)) {
                for (var j = 0; j < line.details.length; j++) {
                    var d = line.details[j];
                    details.push({
                        PatientName: d.PatientName,
                        Beneficiary: d.Beneficiary,
                        TreatmentCategory: d.TreatmentCategory,
                        SubCategory: d.SubCategory,
                        Diagnosa: d.Diagnosa,
                        StartEndDate: d.StartEndDate,
                        InvoiceAmount: d.InvoiceAmount,
                        ActualAmount: d.ActualAmount,
                        Saving: d.Saving,
                        Deduction: d.Deduction
                    });
                }
            }

            payload.lines.push({
                employee: empVal,
                division: line.division,
                department: line.department,
                position: line.position,
                costname: line.costname,
                details: details
            });
        }

        $.ajax({
            url: '/NGKbusi/HC/Medical/SubmitClaim',
            type: 'POST',
            contentType: 'application/json; charset=utf-8',
            data: JSON.stringify(payload),
            dataType: 'json',
            beforeSend: function () {
                $('#btnSubmitClaim').prop('disabled', true).text('Saving...');
            },
            success: function (res) {
                if (res && res.success) {
                    //alert('Claim saved (ID: ' + res.id + ').');
                    swal('Success', 'Claim saved successfully.', 'success');
                    // Update active ID so future saves are updates
                    activeClaimId = res.id;
                    loadMedicalList();
                    $(".divMedicalClaimForm").hide();
                    setTimeout(setFormClean, 500);
                } else {
                    alert('Save failed: ' + (res && res.message ? res.message : 'unknown'));
                }
            },
            error: function () {
                alert('Error while saving claim.');
            },
            complete: function () {
                $('#btnSubmitClaim').prop('disabled', false).text('Submit Claim');
            }
        });
    });

    $("#selTreatmentCategory").change(function () {
        var currCategory = $(this).val();
        var subCategories = subCategoryData.find(item => item.category === currCategory)?.subCategory || [];
        var $subCategorySelect = $("#selSubCategory");
        $subCategorySelect.find("option[value!='']").remove();
        $.each(subCategories, function (index, value) { $subCategorySelect.append($('<option>', { value: value, text: value })); });
    });

    // refresh list
    $('#btnRefreshClaims').on('click', function () { loadMedicalList(); });

    // initial load
    loadMedicalList();
});

    // Optionally, hide warning when form is closed or reset in other ways