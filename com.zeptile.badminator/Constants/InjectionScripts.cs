namespace com.zeptile.badminator.Constants;

public static class InjectionScripts
{
    public const string GetAllBadmintonAvailabilityForPage =
        @"() => {
            const results = [];
            const tableBodyEl = document.querySelector('#u5200_tableTableActivitySearch>tbody');
          
            tableBodyEl.querySelectorAll('tr').forEach((tr) => {

                const cells = tr.querySelectorAll('td');

                let status;

                switch (cells[6].firstChild.innerText) {
                    case 'Complet':
                        status = 0;
                        break;
                    case 'En cours':
                        status = 1;
                        break;
                    case 'À venir':
                        status = 2;
                        break;
                    case 'Terminé':
                        status = 3;
                        break;
                    case 'Plus d\'informations':
                        status = 4;
                        break;
                    default:
                        status = 5;
                }
                
                results.push({
                    Code: cells[1].innerText,
                    Site: cells[5].innerText,
                    Schedule: cells[4].firstChild.innerText,
                    StartDate: cells[3].firstChild.data,
                    EndDate: cells[3].lastChild.data,
                    Status: status
                });
            });
          
	        return results;
        }";
}