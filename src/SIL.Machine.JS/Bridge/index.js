require('./lib/bridge');
require('./lib/newtonsoft.json');
require('./lib/machine');

var machine = {};

function addClass(ns, cls)
{
    machine[cls] = ns[cls];
}

addClass(SIL.Machine.Translation, 'TranslationEngine');
addClass(SIL.Machine.Translation, 'InteractiveTranslationSession');
addClass(SIL.Machine.Translation, 'SmtTrainProgress');
addClass(SIL.Machine.Tokenization, 'SegmentTokenizer');
addClass(SIL.Machine.Project, 'TranslationProjectManager');

module.exports = machine;