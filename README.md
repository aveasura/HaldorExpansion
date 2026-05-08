# Haldor Expansion

Мод для Valheim на BepInEx + Jotunn, который расширяет торговлю у Haldor и добавляет уникальные предметы с кастомными боевыми механиками.

## Установка для игроков

Для обычной установки не нужно менять код, настраивать `.csproj`, создавать `HaldorExpansion.Local.props` или отдельно копировать PNG-файлы.

Достаточно положить скомпилированный файл:

```text
HaldorExpansion.dll
```

в папку плагина:

```text
BepInEx/plugins/aveasura-HaldorExpansion/
```

Игровые иконки предметов уже встроены внутрь `HaldorExpansion.dll` как embedded resources.

> `HaldorExpansion.Local.props` нужен только разработчикам для локальной сборки проекта. Игрокам он не нужен.

## Важно для multiplayer / dedicated server

Мод меняет геймплей и добавляет сетевые механики, поэтому на dedicated server его должны иметь и сервер, и все клиенты.

Рекомендуется использовать одинаковую версию мода на сервере и у всех игроков.

## Что добавляет

- новые товары у Haldor;
- возможность продавать трофеи и некоторые редкие материалы, включая `SurtlingCore`, `BlackCore` и `MoltenCore`;
- уникальные предметы с кастомными боевыми механиками;
- кастомные броню и плащи с рискованными эффектами;
- поддержку multiplayer / dedicated server через `NetworkCompatibility(EveryoneMustHaveMod, Minor)`;
- русскую и английскую локализации для добавленных предметов и эффектов.

## Добавленные уникальные предметы

- `Костоломы / Bone Crushers` - кастеты с активной способностью, защитным покровом и ударной волной;
- `Кираса безмолвной расплаты / Cuirass of Silent Reckoning` - броня, которая отсрочивает часть опасного урона;
- `Плащ раненого зверя / Cloak of the Wounded Beast` - плащ с рискованной регенерацией на низком здоровье;
- `Плащ выжженной стойкости / Cloak of Burned Resolve` - плащ, переводящий часть урона в расход выносливости;
- `Осквернённый Бризингамен / Corrupted Brisingamen` - аксессуар для переносимого веса;
- `Арбалет теневой охоты / Crossbow of the Shadow Hunt` - тяжёлый арбалет для мощного первого выстрела;
- `Накидка хозяина ямы / Pit King's Cuirass` - предмет для агрессивной игры с кастетом.

## Рекомендуемый стиль игры

Мод лучше всего раскрывается на повышенной сложности, где дополнительные предметы ощущаются не как бесплатное усиление, а как рискованные инструменты выживания.

Для более хардкорного прохождения хорошо сочетается с:

- `Smoothbrain-CreatureLevelAndLootControl` - усиление существ;
- `warpalicious-Monster_Modifiers` - дополнительные модификаторы существ;
- `ASharpPen-Custom_Raids` - более опасные и частые рейды.

Эти моды не являются обязательными зависимостями.

## Совместимость

Обязательные зависимости:

- BepInEx 5;
- Jotunn.

Мод тестировался в multiplayer / dedicated server окружении вместе с другими gameplay-модами, включая:

- `Smoothbrain-CreatureLevelAndLootControl`;
- `warpalicious-Monster_Modifiers`;
- `ASharpPen-Custom_Raids`;
- `Azumatt-AzuExtendedPlayerInventory`;
- `Azumatt-AzuSkillTweaks`;
- `rendl0449-CraftFromContainers`;
- `Advize-PlantEverything`;
- `Marf-FuelEternal`.

Полная совместимость со всеми версиями сторонних модов не гарантируется, но в моей тестовой сборке конфликтов не обнаружено.

## Runtime assets

Игровые иконки предметов находятся внутри `HaldorExpansion.dll` как embedded resources.

Игрокам не нужно отдельно копировать PNG-файлы.

Исходные PNG-файлы лежат в проекте в папке:

```text
HaldorExpansion/Assets/Icons/
```

Они нужны только для сборки проекта из исходников.

## Требования для разработки

- Valheim;
- BepInEx 5;
- Jotunn;
- .NET Framework 4.8;
- Visual Studio или Rider.

Проект использует локальные пути к Valheim/r2modman DLL в `HaldorExpansion/HaldorExpansion.csproj`. Пути вынесены в MSBuild-свойства:

- `R2ModmanProfileDir`;
- `ValheimInstallDir`;
- `ValheimDedicatedServerDir`;
- `ClientPluginDeployDir`;
- `ServerPluginDeployDir`.

Если папки отличаются, используйте `HaldorExpansion.Local.props`, как описано ниже в разделе Local build paths.

## Local build paths

Если пути к Valheim, r2modman или dedicated server отличаются от дефолтных в `.csproj`, создайте локальный файл настроек:

```text
HaldorExpansion/HaldorExpansion.Local.props
```

Для этого можно скопировать готовый пример:

```text
HaldorExpansion/HaldorExpansion.Local.props.example
```

и переименовать копию в:

```text
HaldorExpansion/HaldorExpansion.Local.props
```

После этого поменяйте пути внутри `HaldorExpansion.Local.props` под себя.

`HaldorExpansion.Local.props` не должен попадать в Git, потому что содержит локальные пути конкретной машины.

## Статус

Текущая версия: рабочая development/test сборка. Кодовая база продолжает дорабатываться и очищаться.
