# Вклад (CONTRIBUTING)

### Даунстриминг

Данный проект является даунстримом [Space Station 14](https://github.com/space-wizards/space-station-14), и планирует оставаться таким.

В связи с этой благой целью, при внесении изменений в проект следует придерживаться процедуры даунстриминга:
1. Если ваши изменения можно внести в [апстрим](https://github.com/space-wizards/space-station-14), это необходимо сделать.
2. Если ваши изменения можно вынести в модуль, который не будет изменять никакой код вне него, это необходимо сделать.
3. Вне-модульные изменения необходимо отмечать следующими тэгами:
- В случае если код добавляется: `//<downstream-add>` до изменений, и `//</downstream-add>` после.
- В случае если код удаляется: `/*<downstream-del>` до изменений, и `</downstream-del>*/` после.
- В случае если вы изменяете какой-то фрагмент апстрим кода, сначала необходимо указать что было удалено, а после что было добавлено:
    /*<downstream-del>
    *Здесь апстрим код что вы удаляете*
    </downstream-del>*/
    //<downstream-add>
    *Здесь код что вы добавляете*
    //</downstream-add>
