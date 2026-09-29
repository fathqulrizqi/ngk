$(document).ready(function () {

    if ($("#tblLCGeneralLedger").length) {
        $("#tblLCGeneralLedger").tablesorter({
            theme: 'metro-dark',
            widthFixed: true,
            widgets: ['zebra', 'columns', 'filter', 'stickyHeaders', 'output','math'],
            widgetOptions: {
                stickyHeaders_attachTo: $("#tblLCGeneralLedger").parent(),
                stickyHeaders_yScroll: $(window),
                output_delivery: 'download',
                output_saveFileName: 'Labor_Cost_GL.csv',
                output_replaceCR: '&amp;',
                output_replaceTab: 'asfaf',
                output_callback: function (config, data, url) {
                    window.open("/NGKbusi/FA/LaborCost/GeneralLedgerDelete");
                    $("#tblLCGeneralLedger").hide();
                    return true;
                },
                filter_saveFilters: true,
                filter_searchDelay: 750,
                math_data: 'math', // data-math attribute
                math_ignore: [0, 1, 2, 3,4,5,6,7,8],
                math_none: 'N/A', // no matching math elements found (text added to cell)
                math_complete: function ($cell, wo, result, value, arry) {
                    var txt = '<span class="align-decimal">' +
                        (value === wo.math_none ? '' : '') +
                        result + '</span>';
                    if ($cell.attr('data-math') === 'all-sum') {
                        // when the "all-sum" is processed, add a count to the end
                        //return txt + ' (Sum of ' + arry.length + ' cells)';
                        return txt;
                    }
                    return txt;
                },
                math_completed: function (c) {
                    // c = table.config
                    // called after all math calculations have completed
                    console.log('math calculations complete', c.$table.find('[data-math="all-sum"]:first').text());
                },
                // see "Mask Examples" section
                math_mask: '#,###.##',
                math_prefix: '', // custom string added before the math_mask value (usually HTML)
                math_suffix: '', // custom string added after the math_mask value
                // event triggered on the table which makes the math widget update all data-math cells (default shown)
                math_event: 'recalculate',
                // math calculation priorities (default shown)... rows are first, then column above/below,
                // then entire column, and lastly "all" which is not included because it should always be last
                math_priority: ['row', 'above', 'below', 'col'],
                // set row filter to limit which table rows are included in the calculation (v2.25.0)
                // e.g. math_rowFilter : ':visible:not(.filtered)' (default behavior when math_rowFilter isn't set)
                // or math_rowFilter : ':visible'; default is an empty string
                math_rowFilter: '',
            }
        }).tablesorterPager({
            container: $(".pager"),
            output: '{startRow} to {endRow} ({totalRows})'
        });
    }
    $('#btnDownload').on("click", function () {
        $("#tblLCGeneralLedger").trigger('outputTable');
    });
});