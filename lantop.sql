-- phpMyAdmin SQL Dump
-- version 4.6.3
-- https://www.phpmyadmin.net/
--
-- Generation Time: 2016-09-12 18:32:56
-- 服务器版本： 5.7.9-log
-- PHP Version: 5.5.11

SET SQL_MODE = "NO_AUTO_VALUE_ON_ZERO";
SET time_zone = "+00:00";


/*!40101 SET @OLD_CHARACTER_SET_CLIENT=@@CHARACTER_SET_CLIENT */;
/*!40101 SET @OLD_CHARACTER_SET_RESULTS=@@CHARACTER_SET_RESULTS */;
/*!40101 SET @OLD_COLLATION_CONNECTION=@@COLLATION_CONNECTION */;
/*!40101 SET NAMES utf8mb4 */;

--
-- Database: `lantop`
--

-- --------------------------------------------------------

--
-- 表的结构 `blacklist`
--

CREATE TABLE `blacklist` (
  `ID` int(10) UNSIGNED NOT NULL,
  `Phone` varchar(11) NOT NULL,
  `Name` varchar(45) NOT NULL,
  `Regard` varchar(45) DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- --------------------------------------------------------

--
-- 表的结构 `config`
--

CREATE TABLE `config` (
  `ID` int(10) UNSIGNED NOT NULL,
  `Key` varchar(45) NOT NULL,
  `Value` text NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

--
-- 转存表中的数据 `config`
--

INSERT INTO `config` (`ID`, `Key`, `Value`) VALUES
(1, 'LanTopUserName', ''),
(2, 'LanTopUserPassword', ''),
(3, 'VerifyUserList', '13816285709,18217739521'),
(4, 'AutoVerifyEndTime', '22:00:00'),
(5, 'AutoVerifyStartTime', '06:00:00');
(6, 'QQClientQun', 'S.I.A-Network Department:1407304440');
(7, 'NeedQQClient', 'true');

-- --------------------------------------------------------

--
-- 表的结构 `contacts`
--

CREATE TABLE `contacts` (
  `id` int(8) NOT NULL,
  `name` varchar(40) NOT NULL,
  `phone` varchar(11) NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- --------------------------------------------------------

--
-- 表的结构 `groups`
--

CREATE TABLE `groups` (
  `id` varchar(40) NOT NULL,
  `contacts_id` int(11) NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- --------------------------------------------------------

--
-- 表的结构 `groups_name`
--

CREATE TABLE `groups_name` (
  `id` varchar(40) NOT NULL,
  `name` varchar(40) NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- --------------------------------------------------------

--
-- 表的结构 `keyword`
--

CREATE TABLE `keyword` (
  `ID` int(10) UNSIGNED NOT NULL,
  `Content` varchar(45) NOT NULL,
  `Regard` varchar(100) DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- --------------------------------------------------------

--
-- 表的结构 `message`
--

CREATE TABLE `message` (
  `ID` int(10) UNSIGNED NOT NULL,
  `User` varchar(45) NOT NULL,
  `Content` varchar(300) NOT NULL,
  `Recipients` text NOT NULL,
  `IsVerify` tinyint(1) NOT NULL DEFAULT '0',
  `IsSend` tinyint(1) NOT NULL DEFAULT '0',
  `IsSubmit` tinyint(1) NOT NULL DEFAULT '0',
  `SubTime` timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `SendTime` datetime DEFAULT NULL,
  `IsQueue` tinyint(1) NOT NULL DEFAULT '0',
  `Auditor` varchar(45) NOT NULL DEFAULT 'System',
  `VerifyTime` datetime DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- --------------------------------------------------------

--
-- 替换视图以便查看 `messagereject`
-- (See below for the actual view)
--
CREATE TABLE `messagereject` (
`User` varchar(45)
,`reject` bigint(21)
);

-- --------------------------------------------------------

--
-- 替换视图以便查看 `messagestatistics`
-- (See below for the actual view)
--
CREATE TABLE `messagestatistics` (
`用户名` varchar(40)
,`提交总数` bigint(21)
,`审核通过` bigint(21)
,`审核拒绝` bigint(21)
,`拒绝率` decimal(24,4)
);

-- --------------------------------------------------------

--
-- 替换视图以便查看 `messagetotal`
-- (See below for the actual view)
--
CREATE TABLE `messagetotal` (
`User` varchar(45)
,`total` bigint(21)
);

-- --------------------------------------------------------

--
-- 替换视图以便查看 `messageverify`
-- (See below for the actual view)
--
CREATE TABLE `messageverify` (
`User` varchar(45)
,`verify` bigint(21)
);

-- --------------------------------------------------------

--
-- 表的结构 `receive`
--

CREATE TABLE `receive` (
  `ID` int(10) UNSIGNED NOT NULL,
  `Phone` varchar(11) NOT NULL,
  `Content` text NOT NULL,
  `Time` datetime NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- --------------------------------------------------------

--
-- 表的结构 `user_permissions`
--

CREATE TABLE `user_permissions` (
  `id` int(11) NOT NULL,
  `username` varchar(40) NOT NULL,
  `permissions` int(11) NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- --------------------------------------------------------

--
-- 视图结构 `messagereject`
--
DROP TABLE IF EXISTS `messagereject`;

CREATE ALGORITHM=UNDEFINED DEFINER=`root`@`%` SQL SECURITY DEFINER VIEW `messagereject`  AS  select `message`.`User` AS `User`,count(`message`.`ID`) AS `reject` from `message` where ((`message`.`IsVerify` = 0) and (`message`.`IsSubmit` = 1)) group by `message`.`User` ;

-- --------------------------------------------------------

--
-- 视图结构 `messagestatistics`
--
DROP TABLE IF EXISTS `messagestatistics`;

CREATE ALGORITHM=UNDEFINED DEFINER=`root`@`%` SQL SECURITY DEFINER VIEW `messagestatistics`  AS  select `a`.`username` AS `用户名`,(case when isnull(`b`.`total`) then 0 else `b`.`total` end) AS `提交总数`,(case when isnull(`c`.`verify`) then 0 else `c`.`verify` end) AS `审核通过`,(case when isnull(`d`.`reject`) then 0 else `d`.`reject` end) AS `审核拒绝`,(case when isnull((`d`.`reject` / `b`.`total`)) then 0 else (`d`.`reject` / `b`.`total`) end) AS `拒绝率` from (((`user_permissions` `a` left join `messagetotal` `b` on((`a`.`username` = `b`.`User`))) left join `messageverify` `c` on((`a`.`username` = `c`.`User`))) left join `messagereject` `d` on((`a`.`username` = `d`.`User`))) order by (case when isnull((`d`.`reject` / `b`.`total`)) then 0 else (`d`.`reject` / `b`.`total`) end) desc ;

-- --------------------------------------------------------

--
-- 视图结构 `messagetotal`
--
DROP TABLE IF EXISTS `messagetotal`;

CREATE ALGORITHM=UNDEFINED DEFINER=`root`@`%` SQL SECURITY DEFINER VIEW `messagetotal`  AS  select `message`.`User` AS `User`,count(`message`.`ID`) AS `total` from `message` group by `message`.`User` ;

-- --------------------------------------------------------

--
-- 视图结构 `messageverify`
--
DROP TABLE IF EXISTS `messageverify`;

CREATE ALGORITHM=UNDEFINED DEFINER=`root`@`%` SQL SECURITY DEFINER VIEW `messageverify`  AS  select `message`.`User` AS `User`,count(`message`.`ID`) AS `verify` from `message` where (`message`.`IsVerify` = 1) group by `message`.`User` ;

--
-- Indexes for dumped tables
--

--
-- Indexes for table `blacklist`
--
ALTER TABLE `blacklist`
  ADD PRIMARY KEY (`ID`);

--
-- Indexes for table `config`
--
ALTER TABLE `config`
  ADD PRIMARY KEY (`ID`),
  ADD UNIQUE KEY `Key` (`Key`),
  ADD KEY `ID` (`ID`),
  ADD KEY `ID_2` (`ID`),
  ADD KEY `Key_2` (`Key`);

--
-- Indexes for table `contacts`
--
ALTER TABLE `contacts`
  ADD PRIMARY KEY (`id`),
  ADD KEY `id` (`id`),
  ADD KEY `id_2` (`id`);

--
-- Indexes for table `groups`
--
ALTER TABLE `groups`
  ADD KEY `id` (`id`),
  ADD KEY `contacts_id` (`contacts_id`);

--
-- Indexes for table `groups_name`
--
ALTER TABLE `groups_name`
  ADD PRIMARY KEY (`id`),
  ADD KEY `id` (`id`);

--
-- Indexes for table `keyword`
--
ALTER TABLE `keyword`
  ADD PRIMARY KEY (`ID`);

--
-- Indexes for table `message`
--
ALTER TABLE `message`
  ADD PRIMARY KEY (`ID`),
  ADD KEY `ID` (`ID`);

--
-- Indexes for table `receive`
--
ALTER TABLE `receive`
  ADD PRIMARY KEY (`ID`),
  ADD KEY `ID` (`ID`),
  ADD KEY `Time` (`Time`),
  ADD KEY `ID_2` (`ID`);

--
-- Indexes for table `user_permissions`
--
ALTER TABLE `user_permissions`
  ADD PRIMARY KEY (`id`);

--
-- 在导出的表使用AUTO_INCREMENT
--

--
-- 使用表AUTO_INCREMENT `blacklist`
--
ALTER TABLE `blacklist`
  MODIFY `ID` int(10) UNSIGNED NOT NULL AUTO_INCREMENT;
--
-- 使用表AUTO_INCREMENT `config`
--
ALTER TABLE `config`
  MODIFY `ID` int(10) UNSIGNED NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=6;
--
-- 使用表AUTO_INCREMENT `contacts`
--
ALTER TABLE `contacts`
  MODIFY `id` int(8) NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=20003175;
--
-- 使用表AUTO_INCREMENT `keyword`
--
ALTER TABLE `keyword`
  MODIFY `ID` int(10) UNSIGNED NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=8;
--
-- 使用表AUTO_INCREMENT `message`
--
ALTER TABLE `message`
  MODIFY `ID` int(10) UNSIGNED NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=967;
--
-- 使用表AUTO_INCREMENT `user_permissions`
--
ALTER TABLE `user_permissions`
  MODIFY `id` int(11) NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=65;
/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40101 SET CHARACTER_SET_RESULTS=@OLD_CHARACTER_SET_RESULTS */;
/*!40101 SET COLLATION_CONNECTION=@OLD_COLLATION_CONNECTION */;
